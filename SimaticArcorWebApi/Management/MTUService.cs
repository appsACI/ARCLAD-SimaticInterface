using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.BOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MaterialLot;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Simatic.Equipment;
using Nancy;
using SimaticWebApi.Model.Custom.PrintLabel;

namespace SimaticArcorWebApi.Management
{
    public class MTUService : IMTUService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<MTUService> logger;

        public ISimaticService SimaticService { get; set; }
        public ISimaticMTUService SimaticMTUService { get; set; }

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public ISimaticEquipmentService SimaticEquipmentService { get; set; }



        public MTUService(ISimaticService simaticService, ISimaticMTUService simaticMTUService, ISimaticMaterialService simaticMaterialService, ISimaticEquipmentService simaticEquipment)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<MTUService>();

            SimaticService = simaticService;
            SimaticMTUService = simaticMTUService;
            SimaticEquipmentService = simaticEquipment;
            SimaticMaterialService = simaticMaterialService;
        }

        public async Task UpdateLotAsync(MTURequest req, CancellationToken ct)
        {
            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

            double quantity = double.Parse(req.Quantity?.QuantityString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

            logger.LogInformation($"Updating lot: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}].");

            //Get Material by ID
            Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, true, ct);

            Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(req.Location.EquipmentID, true, ct);

            MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

            MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

            if (mlot == null)
            {
                var mlotId = await SimaticMTUService.CreateMaterialLot(req.Id, mat.NId, mat.Description, mat.UoMNId, quantity, ct);
                logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
            }

            if (mtu == null)
            {
                logger.LogInformation($"MTU not found, creating one");

                await SimaticMTUService.CreateMaterialTrackingUnitAsync(req.Id, mat.NId, mat.Description, eq.NId, mat.UoMNId, quantity, req.MaterialLotProperty, ct);

                logger.LogInformation($"MTU has been created.");

                //await SimaticMTUService.TriggerNewMaterialTrackingUnitEventAsync(req.Id, ct);

                // Update info
                mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);
                //logger.LogInformation($"Event triggered.");
            }
            else
            {
                logger.LogInformation($"Updating MTU");


                var restQuantity = quantity;

                logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{quantity}] total value [{restQuantity}]");
                await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, restQuantity, ct);
                mtu.Quantity.QuantityValue = restQuantity;

                logger.LogInformation($"Updating material Lot quantity id [{mtu.NId}] value [{restQuantity}]");
                await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, restQuantity, ct);


                //if (!string.IsNullOrEmpty(req.Status) && req.Status != mtu.Status.StatusNId)
                //{
                //  logger.LogInformation($"Updating MTU status, id [{mtu.NId}] old status [{mtu.Status.StatusNId}], new status [{req.Status}]");
                //  await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, req.Status, ct);
                //}

                if (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId)
                {
                    logger.LogInformation($"Moving MTU, id [{mtu.NId}] from [{mtu.EquipmentNId}], to [{eq.NId}]");
                    await SimaticMTUService.MoveMaterialTrackingUnitToEquipmentAsync(mtu.Id, eq.NId, ct);
                }
            }

            await CreateOrUpdateMTUProperties(mtu.Id, req.MaterialLotProperty, ct);
        }

        public async Task DescountQuantity(MTUDescount req, CancellationToken ct)
        {

            //logger.LogInformation($"Updating lot: Id [{req.MTUNId}], Quantity [{req.Quantity}].");

            //Get Material by ID
            MTUAsignedArray[] mtuAsigned = await SimaticMTUService.GetMTUAsigned(req.WorkOrderNId, ct);

            if (mtuAsigned == null)
            {
                logger.LogInformation($"MTUAsigned not found, not exist");

            }
            else
            {

                foreach (MTUAsigned item in mtuAsigned[0].MaterialList)
                {
                    logger.LogInformation($"MTUNId: {item.Mtu}");

                    MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(item.Mtu, ct);

                    MaterialLot mlot = await SimaticMTUService.GetMaterialLot(item.Mtu, ct);

                    if (mtu == null)
                    {
                        logger.LogInformation($"MTU not found, not exist");
                    }
                    else
                    {
                        for (var i = 0; i < req.MtuInfo.Length; i++)
                        {
                            if (item.Mtu == req.MtuInfo[i].MtuNId)
                            {
                                logger.LogInformation($"Updating MTU");

                                var restQuantity = mtu.Quantity.QuantityValue - req.MtuInfo[i].Quantity;

                                if (restQuantity <= 0)
                                {
                                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{req.MtuInfo[0].Quantity}] total value [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, 0, ct);
                                    mtu.Quantity.QuantityValue = restQuantity;

                                    logger.LogInformation($"Updating material Lot id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{req.MtuInfo[0].Quantity}] total value [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, 0, ct);

                                    //await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, "Liberar", ct);

                                }
                                else
                                {

                                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{req.MtuInfo[0].Quantity}] total value [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, restQuantity, ct);
                                    mtu.Quantity.QuantityValue = restQuantity;

                                    logger.LogInformation($"Updating material Lot id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{req.MtuInfo[0].Quantity}] total value [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, restQuantity, ct);
                                }


                                var restAsigned = Convert.ToDouble(item.MtuQuantity) - req.MtuInfo[i].Quantity;

                                if (restAsigned <= 0)
                                {
                                    logger.LogInformation($"Quantity [{restAsigned}] procced Unassign MTU [{item.Mtu}] for Work Order [{req.WorkOrderNId}]");
                                    await SimaticMTUService.UnassignMTURequired(item.Id, ct);

                                }
                                else
                                {
                                    logger.LogInformation($"Updating MTU Asigned quantity id [{item.Mtu}] new value [{restAsigned}]");
                                    await SimaticMTUService.UpdateMTUAsigned(item.Id, restAsigned, ct);
                                }

                            }

                        }

                    }
                }

            }
        }

        public async Task AssignedUnassign(MTUAssignedUnassign req, CancellationToken ct)
        {
            logger.LogInformation($"Reciveb MTUNid's for Unassign");
            MTUAsignedArray[] mtuAsigned = await SimaticMTUService.GetMTUAsigned(req.WorkOrderNid, ct);

            foreach (MTUAsigned mtuReq in mtuAsigned[0].MaterialList)
            {
                foreach (var item in req.MTUNid)
                {
                    if (mtuReq.Mtu == item)
                    {

                        logger.LogInformation($"Procced search MTU id");

                        MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(item, ct);
                        var workOrder = await SimaticMTUService.GetPropertyWorkOrder(item, ct);
                        IList<MTUPropiedadesDefectos> defectos = await GetPropertiesDefectos(item, ct);
                        var maquinaDeCreacion = await SimaticMTUService.GetMaquinaDeCreacion(item, ct);

                        if (mtu != null)
                        {
                            #region Print Label

                            PrintModel label;

                            if (mtu.MaterialNId.StartsWith("1") || mtu.MaterialNId.StartsWith("21") || mtu.MaterialNId.StartsWith("22") || mtu.MaterialNId.StartsWith("23"))
                            {
                                label = new PrintModel
                                {
                                    LabelTagsArseg = new ARSEGPrintModel { }, //este objeto es solo para arseg
                                    LabelTagsOther = new OtherTagsModel { }, //Este Objeto es para otras etiquetas
                                    LabelTagsRollos = new RolloPrintModel { },//Este objeto es solo para rollos
                                    LabelTagsRecubrimiento = new RecubrimientoModel { }, //este objeto es solo para recubrimientos
                                    LabelPrinter = req.Printer ?? "PRINT1",
                                    NoOfCopies = "1",
                                    LabelTagsMezclas = new MezclasPrintModel
                                    {
                                        TagCantidad = mtuReq.MtuQuantity,
                                        TagDatetime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                                        TagDescripcion = "", //este debe ir vacio
                                        Tagfecha_vcto = "",
                                        TagLote = $"{item}",
                                        TagLote_proveedor = "",
                                        TagMaterial = $"{mtuReq.MaterialNId}",
                                        TagObservaciones = " ",
                                        TagOperario = req.Operario,
                                        TagOrdenCompra = " ", //no tenemos el dato
                                        TagProveedor = " ", //no tenemos el dato
                                        TagReferencia = " ", //este debe ir vacio
                                        TagUom = " " //este debe ir vacio
                                    },
                                    LabelTemplate = "Materiaprima",
                                    LabelType = "ProductoenProcesoMezclasLbl",
                                    PrintOperation = "Mezclas"
                                };
                            }
                            else if (mtu.MaterialNId.StartsWith("3"))
                            {
                                var date = DateTime.Now;
                                label = new PrintModel
                                {
                                    LabelPrinter = req.Printer ?? "PRINT1",
                                    LabelTagsArseg = new ARSEGPrintModel { },
                                    LabelTagsMezclas = new MezclasPrintModel { },
                                    LabelTagsOther = new OtherTagsModel { },
                                    LabelTagsRecubrimiento = new RecubrimientoModel { },
                                    LabelTagsRollos = new RolloPrintModel
                                    {
                                        TagPtacnho = " ",
                                        TagPtanchodecimales = " ",
                                        TagPtcantidad = $"{mtuReq.MtuQuantity}",
                                        TagPtdate = date.ToString("dd-MM-yyyy"),
                                        TagPtdatetime = date.ToString("dd-MM-yyyy HH:mm:ss"),
                                        TagPtdescripcion = " ",
                                        TagPtdimensiones = " ",
                                        TagPtidmaterial = $"{mtuReq.MaterialNId}",
                                        TagPtlargo = " ",
                                        TagPtlote = item,
                                        TagPtmadeincolombia = " ",
                                        TagPtoperario = req.Operario,
                                        TagPtordendeproduccion = workOrder.PropertyValue,
                                        TagPtpesoaproximado = " ",
                                        TagPtqr = " ",
                                        TagPtreferencia = " ",
                                        TagPtresumenrollo = " ",
                                        TagPtrollo = item,
                                        TagPtuoman = " ",
                                        TagPtuomlg = " "
                                    },
                                    LabelTemplate = "productoterminadozpl",
                                    LabelType = "productoTerminadoLbl",
                                    NoOfCopies = "1",
                                    PrintOperation = "Rollos"
                                };
                            }
                            else
                            {
                                var date = DateTime.Now;
                                label = new PrintModel
                                {
                                    LabelTagsRecubrimiento = new RecubrimientoModel
                                    {
                                        TagPpmts2defec = " ", //Total de 'M2' de defecto del rollo
                                        TagPpmtslindefc = " ", //Total de 'M' de defecto del rollo 
                                        TagPpmtspra = $"{MetrosCuadradosAMetrosLinealesPorMaterial(Convert.ToDouble(mtuReq.MtuQuantity), mtuReq.MaterialNId).ToString("F4")}", //Total 'M' de la orden
                                        TagPpmts2pra = $"{mtuReq.MtuQuantity}", // Total 'M2' de la order
                                        TagPpobservaciones = " ", //Observaciones de la orden
                                        TagPpoperario = req.Operario, //Nombre operario
                                        TagPporden = workOrder[0].PropertyValue, //Orden de trabajo
                                        TagPpreferencia = $"{mtuReq.MaterialNId}", //Material de la orden
                                        TagPpresponsable = " ", //este debe ir vacio
                                        TagPpmetroscuadrados = $"{mtuReq.MtuQuantity}", //'M2' para el Qr
                                        TagPpcodigo = " ", //este debe ir vacio
                                        TagPpdate = date.ToString("dd-MM-yyyy"), //Fecha de la operación
                                        TagPpfechaformato = " ", //este debe ir vacio
                                        TagPphora = date.ToString("HH:mm:ss"), //Hora de la operación
                                        TagPpid = $"{mtuReq.MaterialNId}", //id del Material
                                        TagPplamfint = " ", //Campo final del turno
                                        TagPplote = item, //Lote generado
                                        TagPpmaquina = maquinaDeCreacion[0].PropertyValue, //Equipo de trabajo
                                        #region defectos
                                        //campo metraje inicial
                                        TagPpmi1 = " ",
                                        TagPpmi2 = " ",
                                        TagPpmi3 = " ",
                                        TagPpmi4 = " ",
                                        TagPpmi5 = " ",
                                        TagPpmi6 = " ",
                                        TagPpmi7 = " ",

                                        //campo metraje final
                                        TagPpmf1 = " ",
                                        TagPpmf2 = " ",
                                        TagPpmf3 = " ",
                                        TagPpmf4 = " ",
                                        TagPpmf5 = " ",
                                        TagPpmf6 = " ",
                                        TagPpmf7 = " ",

                                        //campo cod
                                        TagPpcod1 = " ",
                                        TagPpcod2 = " ",
                                        TagPpcod3 = " ",
                                        TagPpcod4 = " ",
                                        TagPpcod5 = " ",
                                        TagPpcod6 = " ",
                                        TagPpcod7 = " ",

                                        //campo Descripción del defecto
                                        TagPpdesdf1 = " ",
                                        TagPpdesdf2 = " ",
                                        TagPpdesdf3 = " ",
                                        TagPpdesdf4 = " ",
                                        TagPpdesdf5 = " ",
                                        TagPpdesdf6 = " ",
                                        TagPpdesdf7 = " ",

                                        //campo G
                                        TagPpg1 = " ",
                                        TagPpg2 = " ",
                                        TagPpg3 = " ",
                                        TagPpg4 = " ",
                                        TagPpg5 = " ",
                                        TagPpg6 = " ",
                                        TagPpg7 = " ",

                                        //campo D
                                        TagPpd1 = " ",
                                        TagPpd2 = " ",
                                        TagPpd3 = " ",
                                        TagPpd4 = " ",
                                        TagPpd5 = " ",
                                        TagPpd6 = " ",
                                        TagPpd7 = " ",

                                        //campo C
                                        TagPpc1 = " ",
                                        TagPpc2 = " ",
                                        TagPpc3 = " ",
                                        TagPpc4 = " ",
                                        TagPpc5 = " ",
                                        TagPpc6 = " ",
                                        TagPpc7 = " ",

                                        //campo metro del defecto
                                        TagPpmd1 = " ",
                                        TagPpmd2 = " ",
                                        TagPpmd3 = " ",
                                        TagPpmd4 = " ",
                                        TagPpmd5 = " ",
                                        TagPpmd6 = " ",
                                        TagPpmd7 = " "
                                        #endregion
                                    },
                                    LabelPrinter = req.Printer ?? "PRINT1",
                                    NoOfCopies = "1",
                                    LabelTagsArseg = new ARSEGPrintModel { }, //este objeto es solo para arseg
                                    LabelTagsOther = new OtherTagsModel { }, //Este Objeto es para otras etiquetas
                                    LabelTagsRollos = new RolloPrintModel { }, //Este objeto es solo para rollos
                                    LabelTagsMezclas = new MezclasPrintModel { }, //este objeto es solo para mezclas
                                    LabelTemplate = "productoenprocesozpl",
                                    LabelType = "ProductoenProcesoLbl",
                                    PrintOperation = "Recubrimiento"
                                };

                                if (defectos != null)
                                {
                                    foreach (MTUPropiedadesDefectos defect in defectos)
                                    {
                                        switch (defect.Id)
                                        {
                                            case 1:
                                                label.LabelTagsRecubrimiento.TagPpmi1 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf1 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod1 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf1 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg1 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc1 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd1 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd1 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 2:
                                                label.LabelTagsRecubrimiento.TagPpmi2 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf2 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod2 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf2 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg2 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc2 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd2 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd2 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 3:
                                                label.LabelTagsRecubrimiento.TagPpmi3 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf3 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod3 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf3 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg3 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc3 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd3 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd3 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 4:
                                                label.LabelTagsRecubrimiento.TagPpmi4 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf4 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod4 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf4 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg4 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc4 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd4 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd4 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 5:
                                                label.LabelTagsRecubrimiento.TagPpmi5 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf5 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod5 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf5 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg5 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc5 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd5 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd5 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 6:
                                                label.LabelTagsRecubrimiento.TagPpmi6 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf6 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod6 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf6 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg6 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc6 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd6 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd6 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                            case 7:
                                                label.LabelTagsRecubrimiento.TagPpmi7 = defect.Metraje_Inicial ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmf7 = defect.Metraje_Final ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpcod7 = defect.Codigo ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpdesdf7 = defect.Descripcion ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpg7 = defect.G ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpc7 = defect.C ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpd7 = defect.O ?? " ";
                                                label.LabelTagsRecubrimiento.TagPpmd7 = $"{defect.Cantidad}" ?? " ";
                                                break;
                                        }
                                    }
                                }
                            }

                            #endregion
                            logger.LogInformation($"Found id [{mtu.Id}] for MTU [{item}] procced Print Label ");
                            await PrintLabel(label, ct);
                            logger.LogInformation($"Found id [{mtu.Id}] for MTU [{item}] procced Unassign ");
                            await SimaticMTUService.UnassignMTURequired(mtuReq.Id, ct);
                            await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, "Liberar", ct);
                            logger.LogInformation($"Mtu unassing OK");

                        }
                        else
                        {
                            logger.LogInformation($"MTU not found");

                        }
                    }
                }


            }



        }

        public async Task TrazabilidadMP(TrazabilidadMPModel req, CancellationToken ct)
        {
            logger.LogInformation($"add Trazabiliti for new MTU [{req.LotId}]");
            foreach (MTUTrazabilidadData item in req.MtuData)
            {
                logger.LogInformation($"add Trazabiliti for MP [{item.MtuNId}]");

                await SimaticMTUService.TrazabilidadMP(req.LotId, req.MaterialNId, req.Quantity, req.UoM, req.User, req.Time, req.EquipmentNId, req.WorkOrderNId, req.WoOperationNId, item.MtuNId, item.Quantity, item.MaterialNId, item.UoM, ct);
            }
        }

        public async Task PrintLabel(PrintModel req, CancellationToken ct)
        {

            if (req.PrintOperation == "Mezclas")
            {
                decimal quantity = req.LabelTagsMezclas.TagCantidad;

                logger.LogInformation($"Printing label lot: Id [{req.LabelTagsMezclas?.TagLote}], Quantity [{quantity}].");

                //Get Material by ID
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsMezclas?.TagMaterial, false, true, ct);

                MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.LabelTagsMezclas?.TagLote, ct);

                req.LabelTagsMezclas.TagUom = mat.UoMNId;
                req.LabelTagsMezclas.TagDescripcion = mat.Name;
                req.LabelTagsMezclas.TagReferencia = mat.Name;

                List<TagModel> TagModelList = new List<TagModel>() { };
                TagModel model = null;
                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagOperario))
                {
                    model = new TagModel
                    {
                        TagName = "operario",
                        TagValue = req.LabelTagsMezclas.TagOperario
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "operario",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagOrdenCompra))
                {
                    model = new TagModel
                    {
                        TagName = "orden_compra",
                        TagValue = req.LabelTagsMezclas.TagOrdenCompra
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "orden_compra",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagProveedor))
                {
                    model = new TagModel
                    {
                        TagName = "proveedor",
                        TagValue = req.LabelTagsMezclas.TagProveedor
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "proveedor",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagReferencia))
                {
                    model = new TagModel
                    {
                        TagName = "referencia",
                        TagValue = req.LabelTagsMezclas.TagReferencia
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "referencia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagUom))
                {
                    model = new TagModel
                    {
                        TagName = "uom",
                        TagValue = req.LabelTagsMezclas.TagUom
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "uom",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (req.LabelTagsMezclas.TagCantidad > 0)
                {
                    model = new TagModel
                    {
                        TagName = "cantidad",
                        TagValue = req.LabelTagsMezclas.TagCantidad.ToString()
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "cantidad",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagDatetime))
                {
                    model = new TagModel
                    {
                        TagName = "datetime",
                        TagValue = req.LabelTagsMezclas.TagDatetime
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "datetime",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagDescripcion))
                {
                    model = new TagModel
                    {
                        TagName = "descripcion",
                        TagValue = req.LabelTagsMezclas.TagDescripcion
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "descripcion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.Tagfecha_vcto))
                {
                    model = new TagModel
                    {
                        TagName = "fecha_vcto",
                        TagValue = req.LabelTagsMezclas.Tagfecha_vcto

                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "fecha_vcto",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagLote))
                {
                    model = new TagModel
                    {
                        TagName = "lote",
                        TagValue = req.LabelTagsMezclas.TagLote
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "lote",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagLote_proveedor))
                {
                    model = new TagModel
                    {
                        TagName = "lote_proveedor",
                        TagValue = req.LabelTagsMezclas.TagLote_proveedor
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "lote_proveedor",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagMaterial))
                {
                    model = new TagModel
                    {
                        TagName = "material",
                        TagValue = req.LabelTagsMezclas.TagMaterial
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "material",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagObservaciones))
                {
                    model = new TagModel
                    {
                        TagName = "observaciones",
                        TagValue = req.LabelTagsMezclas.TagObservaciones
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "observaciones",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                logger.LogInformation($"lista de tags: {TagModelList.ToArray()}");


                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "Recubrimiento")
            {

                //    logger.LogInformation($"Printing label lot: Id [{req.LabelTagsMezclas?.TagLote}], Quantity [{quantity}].");

                //Get Material by NI
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsRecubrimiento?.TagPpid, false, true, ct);

                //MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.LabelTagsRecubrimiento?.TagLote, ct);


                req.LabelTagsRecubrimiento.TagPpreferencia = mat.Name;

                List<TagModel> TagModelList = new List<TagModel>() { };
                TagModel model = null;

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpoperario))
                {
                    model = new TagModel
                    {
                        TagName = "ppoperario",
                        TagValue = req.LabelTagsRecubrimiento.TagPpoperario
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppoperario",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPporden))
                {
                    model = new TagModel
                    {
                        TagName = "pporden",
                        TagValue = req.LabelTagsRecubrimiento.TagPporden
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "pporden",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpreferencia))
                {
                    model = new TagModel
                    {
                        TagName = "ppreferencia",
                        TagValue = req.LabelTagsRecubrimiento.TagPpreferencia
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppreferencia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpresponsable))
                {
                    model = new TagModel
                    {
                        TagName = "ppresponsable",
                        TagValue = req.LabelTagsRecubrimiento.TagPpresponsable
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppresponsable",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpobservaciones))
                {
                    model = new TagModel
                    {
                        TagName = "ppobservaciones",
                        TagValue = req.LabelTagsRecubrimiento.TagPpobservaciones
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppobservaciones",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdate))
                {
                    model = new TagModel
                    {
                        TagName = "ppdate",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdate
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdate",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPphora))
                {
                    model = new TagModel
                    {
                        TagName = "pphora",
                        TagValue = req.LabelTagsRecubrimiento.TagPphora
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "pphora",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                //if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpfechaformato))
                //{
                //    model = new TagModel
                //    {
                //        TagName = "Descripcion",
                //        TagValue = req.LabelTagsRecubrimiento.TagPpfechaformato
                //    };
                //    TagModelList.Add(model);

                //}

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpid))
                {
                    model = new TagModel
                    {
                        TagName = "ppid",
                        TagValue = req.LabelTagsRecubrimiento.TagPpid

                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppid",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPplote))
                {
                    model = new TagModel
                    {
                        TagName = "pplote",
                        TagValue = req.LabelTagsRecubrimiento.TagPplote
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "pplote",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPplamfint))
                {
                    model = new TagModel
                    {
                        TagName = "pplamfint",
                        TagValue = req.LabelTagsRecubrimiento.TagPplamfint
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "pplamfint",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmaquina))
                {
                    model = new TagModel
                    {
                        TagName = "ppmaquina",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmaquina
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmaquina",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcodigo))
                {
                    model = new TagModel
                    {
                        TagName = "ppcodigo",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcodigo
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcodigo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmetroscuadrados))
                {
                    model = new TagModel
                    {
                        TagName = "ppmetroscuadrados",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmetroscuadrados
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmetroscuadrados",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmts2defec))
                {
                    model = new TagModel
                    {
                        TagName = "ppmts2defec",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmts2defec
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmts2defec",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmtslindefc))
                {
                    model = new TagModel
                    {
                        TagName = "ppmtslindefc",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmtslindefc
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmtslindefc",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmtspra))
                {
                    model = new TagModel
                    {
                        TagName = "ppmtspra",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmtspra
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmtspra",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmts2pra))
                {
                    model = new TagModel
                    {
                        TagName = "ppmts2pra",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmts2pra
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmts2pra",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                #region Datos de Defectos
                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc1))
                {
                    model = new TagModel
                    {
                        TagName = "ppc1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc2))
                {
                    model = new TagModel
                    {
                        TagName = "ppc2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc3))
                {
                    model = new TagModel
                    {
                        TagName = "ppc3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc4))
                {
                    model = new TagModel
                    {
                        TagName = "ppc4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc5))
                {
                    model = new TagModel
                    {
                        TagName = "ppc5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc6))
                {
                    model = new TagModel
                    {
                        TagName = "ppc6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpc7))
                {
                    model = new TagModel
                    {
                        TagName = "ppc7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpc7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppc7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod1))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod2))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod3))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod4))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod5))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod6))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpcod7))
                {
                    model = new TagModel
                    {
                        TagName = "ppcod7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpcod7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppcod7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd1))
                {
                    model = new TagModel
                    {
                        TagName = "ppd1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd2))
                {
                    model = new TagModel
                    {
                        TagName = "ppd2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd3))
                {
                    model = new TagModel
                    {
                        TagName = "ppd3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd4))
                {
                    model = new TagModel
                    {
                        TagName = "ppd4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd5))
                {
                    model = new TagModel
                    {
                        TagName = "ppd5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd6))
                {
                    model = new TagModel
                    {
                        TagName = "ppd6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpd7))
                {
                    model = new TagModel
                    {
                        TagName = "ppd7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpd7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppd7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf1))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf2))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf3))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf4))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf5))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf6))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpdesdf7))
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpdesdf7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppdesdf7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg1))
                {
                    model = new TagModel
                    {
                        TagName = "ppg1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg2))
                {
                    model = new TagModel
                    {
                        TagName = "ppg2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg3))
                {
                    model = new TagModel
                    {
                        TagName = "ppg3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg4))
                {
                    model = new TagModel
                    {
                        TagName = "ppg4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg5))
                {
                    model = new TagModel
                    {
                        TagName = "ppg5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg6))
                {
                    model = new TagModel
                    {
                        TagName = "ppg6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpg7))
                {
                    model = new TagModel
                    {
                        TagName = "ppg7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpg7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppg7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd1))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd2))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd3))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd4))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd5))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd6))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmd7))
                {
                    model = new TagModel
                    {
                        TagName = "ppmd7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmd7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmd7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf1))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf2))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf3))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf4))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf5))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf6))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmf7))
                {
                    model = new TagModel
                    {
                        TagName = "ppmf7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmf7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmf7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi1))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi1",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi1
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi2))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi2",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi2
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi3))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi3",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi3
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi3",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi4))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi4",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi4
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi4",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi5))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi5",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi5
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi5",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi6))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi6",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi6
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi6",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRecubrimiento.TagPpmi7))
                {
                    model = new TagModel
                    {
                        TagName = "ppmi7",
                        TagValue = req.LabelTagsRecubrimiento.TagPpmi7
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ppmi7",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }
                #endregion


                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "Rollos")
            {
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsRollos.TagPtidmaterial, false, true, ct);

                req.LabelTagsRollos.TagPtdescripcion = mat.Name;
                List<TagModel> TagModelList = new List<TagModel>() { };
                TagModel model = null;

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtacnho))
                {
                    model = new TagModel
                    {
                        TagName = "ptancho",
                        TagValue = req.LabelTagsRollos.TagPtacnho
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptancho",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtanchodecimales))
                {
                    model = new TagModel
                    {
                        TagName = "ptanchodecimales",
                        TagValue = req.LabelTagsRollos.TagPtanchodecimales
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptanchodecimales",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtcantidad))
                {
                    model = new TagModel
                    {
                        TagName = "ptcantidad",
                        TagValue = req.LabelTagsRollos.TagPtcantidad
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcantidad",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtdate))
                {
                    model = new TagModel
                    {
                        TagName = "ptdate",
                        TagValue = req.LabelTagsRollos.TagPtdate
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptdate",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtdatetime))
                {
                    model = new TagModel
                    {
                        TagName = "ptdatetime",
                        TagValue = req.LabelTagsRollos.TagPtdatetime
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptdatetime",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtdescripcion))
                {
                    model = new TagModel
                    {
                        TagName = "ptdescripcion",
                        TagValue = req.LabelTagsRollos.TagPtdescripcion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptdescripcion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtdimensiones))
                {
                    model = new TagModel
                    {
                        TagName = "ptdimensiones",
                        TagValue = req.LabelTagsRollos.TagPtdimensiones
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptdimensiones",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtidmaterial))
                {
                    model = new TagModel
                    {
                        TagName = "ptidmaterial",
                        TagValue = req.LabelTagsRollos.TagPtidmaterial
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptidmaterial",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtlargo))
                {
                    model = new TagModel
                    {
                        TagName = "ptlargo",
                        TagValue = req.LabelTagsRollos.TagPtlargo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptlargo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtlote))
                {
                    model = new TagModel
                    {
                        TagName = "ptlote",
                        TagValue = req.LabelTagsRollos.TagPtlote
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptlote",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtmadeincolombia))
                {
                    model = new TagModel
                    {
                        TagName = "ptmadeincolombia",
                        TagValue = req.LabelTagsRollos.TagPtmadeincolombia
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptmadeincolombia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtoperario))
                {
                    model = new TagModel
                    {
                        TagName = "ptoperario",
                        TagValue = req.LabelTagsRollos.TagPtoperario
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptoperario",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtordendeproduccion))
                {
                    model = new TagModel
                    {
                        TagName = "ptordendeproduccion",
                        TagValue = req.LabelTagsRollos.TagPtordendeproduccion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptordendeproduccion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);

                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtpesoaproximado))
                {
                    model = new TagModel
                    {
                        TagName = "ptpesoaproximado",
                        TagValue = req.LabelTagsRollos.TagPtpesoaproximado
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptpesoaproximado",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtqr))
                {
                    model = new TagModel
                    {
                        TagName = "ptqr",
                        TagValue = req.LabelTagsRollos.TagPtqr
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptqr",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtreferencia))
                {
                    model = new TagModel
                    {
                        TagName = "ptreferencia",
                        TagValue = req.LabelTagsRollos.TagPtreferencia
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptreferencia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtresumenrollo))
                {
                    model = new TagModel
                    {
                        TagName = "ptresumenrollo",
                        TagValue = req.LabelTagsRollos.TagPtresumenrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptresumenrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtrollo))
                {
                    model = new TagModel
                    {
                        TagName = "ptrollo",
                        TagValue = req.LabelTagsRollos.TagPtrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtuoman))
                {
                    model = new TagModel
                    {
                        TagName = "ptuoman",
                        TagValue = req.LabelTagsRollos.TagPtuoman
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptuoman",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtuomlg))
                {
                    model = new TagModel
                    {
                        TagName = "ptuomlg",
                        TagValue = req.LabelTagsRollos.TagPtuomlg
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptuomlg",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtempate))
                {
                    model = new TagModel
                    {
                        TagName = "ptempate",
                        TagValue = req.LabelTagsRollos.TagPtempate
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptempate",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "ARSEG")
            {
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsArseg.TagAridmaterial, false, true, ct);

                req.LabelTagsArseg.TagArreferencia = mat.Name;
                req.LabelTagsArseg.TagArdescripcion = mat.Name;
                req.LabelTagsArseg.TagArdescripcion1 = mat.Name;

                List<TagModel> TagModelList = new List<TagModel>() { };
                TagModel model = null;

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArcantidad))
                {
                    model = new TagModel
                    {
                        TagName = "arcantidad",
                        TagValue = req.LabelTagsArseg.TagArcantidad
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arcantidad",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArcantidadm2))
                {
                    model = new TagModel
                    {
                        TagName = "arcantidadm2",
                        TagValue = req.LabelTagsArseg.TagArcantidadm2
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arcantidadm2",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArcliente))
                {
                    model = new TagModel
                    {
                        TagName = "arcliente",
                        TagValue = req.LabelTagsArseg.TagArcliente
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arcliente",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArcodcliente))
                {
                    model = new TagModel
                    {
                        TagName = "arcodcliente",
                        TagValue = req.LabelTagsArseg.TagArcodcliente
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arcodcliente",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdate))
                {
                    model = new TagModel
                    {
                        TagName = "ardate",
                        TagValue = req.LabelTagsArseg.TagArdate
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardate",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdatetime))
                {
                    model = new TagModel
                    {
                        TagName = "ardatetime",
                        TagValue = req.LabelTagsArseg.TagArdatetime
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardatetime",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdescripcion))
                {
                    model = new TagModel
                    {
                        TagName = "ardescripcion",
                        TagValue = req.LabelTagsArseg.TagArdescripcion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardescripcion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdescripcion1))
                {
                    model = new TagModel
                    {
                        TagName = "ardescripcion1",
                        TagValue = req.LabelTagsArseg.TagArdescripcion1
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardescripcion1",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdimensionalto))
                {
                    model = new TagModel
                    {
                        TagName = "ardimensionalto",
                        TagValue = req.LabelTagsArseg.TagArdimensionalto
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardimensionalto",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArdimensionancho))
                {
                    model = new TagModel
                    {
                        TagName = "ardimensionancho",
                        TagValue = req.LabelTagsArseg.TagArdimensionancho
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ardimensionancho",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagAridmaterial))
                {
                    model = new TagModel
                    {
                        TagName = "aridmaterial",
                        TagValue = req.LabelTagsArseg.TagAridmaterial
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "aridmaterial",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArlote))
                {
                    model = new TagModel
                    {
                        TagName = "arlote",
                        TagValue = req.LabelTagsArseg.TagArlote
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arlote",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArnrodecaja))
                {
                    model = new TagModel
                    {
                        TagName = "arnrodecaja",
                        TagValue = req.LabelTagsArseg.TagArnrodecaja
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arnrodecaja",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArnrollo))
                {
                    model = new TagModel
                    {
                        TagName = "arnrollo",
                        TagValue = req.LabelTagsArseg.TagArnrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arnrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArobservaciones))
                {
                    model = new TagModel
                    {
                        TagName = "arobservaciones",
                        TagValue = req.LabelTagsArseg.TagArobservaciones
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arobservaciones",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagAroperario))
                {
                    model = new TagModel
                    {
                        TagName = "aroperario",
                        TagValue = req.LabelTagsArseg.TagAroperario
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "aroperario",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArordencompra))
                {
                    model = new TagModel
                    {
                        TagName = "arordencompra",
                        TagValue = req.LabelTagsArseg.TagArordencompra
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arordencompra",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArordenproduccion))
                {
                    model = new TagModel
                    {
                        TagName = "arordenproduccion",
                        TagValue = req.LabelTagsArseg.TagArordenproduccion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arordenproduccion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArordenventa))
                {
                    model = new TagModel
                    {
                        TagName = "arordenventa",
                        TagValue = req.LabelTagsArseg.TagArordenventa
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arordenventa",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArpesoaproximado))
                {
                    model = new TagModel
                    {
                        TagName = "arpesoaproximado",
                        TagValue = req.LabelTagsArseg.TagArpesoaproximado
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arpesoaproximado",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArqr))
                {
                    model = new TagModel
                    {
                        TagName = "arqr",
                        TagValue = req.LabelTagsArseg.TagArqr
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arqr",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArreferencia))
                {
                    model = new TagModel
                    {
                        TagName = "arreferencia",
                        TagValue = req.LabelTagsArseg.TagArreferencia
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arreferencia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArtelefono))
                {
                    model = new TagModel
                    {
                        TagName = "artelefono",
                        TagValue = req.LabelTagsArseg.TagArtelefono
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "artelefono",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagArunidadesxcaja))
                {
                    model = new TagModel
                    {
                        TagName = "arunidadesxcaja",
                        TagValue = req.LabelTagsArseg.TagArunidadesxcaja
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arunidadesxcaja",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }
                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "Other")
            {
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsOther.TagPtcidmaterial, false, true, ct);

                req.LabelTagsOther.TagPtcdescripcion = mat.Name;

                List<TagModel> TagModelList = new List<TagModel>() { };
                TagModel model = null;

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcancho))
                {
                    model = new TagModel
                    {
                        TagName = "ptcancho",
                        TagValue = req.LabelTagsOther.TagPtcancho
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcancho",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtccantidad))
                {
                    model = new TagModel
                    {
                        TagName = "ptccantidad",
                        TagValue = req.LabelTagsOther.TagPtccantidad
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptccantidad",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtccodigo))
                {
                    model = new TagModel
                    {
                        TagName = "ptccodigo",
                        TagValue = req.LabelTagsOther.TagPtccodigo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptccodigo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcconsecutivoestiba))
                {
                    model = new TagModel
                    {
                        TagName = "ptcconsecutivoestiba",
                        TagValue = req.LabelTagsOther.TagPtcconsecutivoestiba
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcconsecutivoestiba",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcdate))
                {
                    model = new TagModel
                    {
                        TagName = "ptcdate",
                        TagValue = req.LabelTagsOther.TagPtcdate
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcdate",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcdatetime))
                {
                    model = new TagModel
                    {
                        TagName = "ptcdatetime",
                        TagValue = req.LabelTagsOther.TagPtcdatetime
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcdatetime",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcdescripcion))
                {
                    model = new TagModel
                    {
                        TagName = "ptcdescripcion",
                        TagValue = req.LabelTagsOther.TagPtcdescripcion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcdescripcion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcidmaterial))
                {
                    model = new TagModel
                    {
                        TagName = "ptcidmaterial",
                        TagValue = req.LabelTagsOther.TagPtcidmaterial
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcidmaterial",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcidrollo))
                {
                    model = new TagModel
                    {
                        TagName = "ptcidrollo",
                        TagValue = req.LabelTagsOther.TagPtcidrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcidrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtclargo))
                {
                    model = new TagModel
                    {
                        TagName = "ptclargo",
                        TagValue = req.LabelTagsOther.TagPtclargo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptclargo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtclote))
                {
                    model = new TagModel
                    {
                        TagName = "ptclote",
                        TagValue = req.LabelTagsOther.TagPtclote
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptclote",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcobservaciones))
                {
                    model = new TagModel
                    {
                        TagName = "ptcobservaciones",
                        TagValue = req.LabelTagsOther.TagPtcobservaciones
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcobservaciones",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcoperario))
                {
                    model = new TagModel
                    {
                        TagName = "ptcoperario",
                        TagValue = req.LabelTagsOther.TagPtcoperario
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcoperario",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcordencompra))
                {
                    model = new TagModel
                    {
                        TagName = "ptcordencompra",
                        TagValue = req.LabelTagsOther.TagPtcordencompra
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcordencompra",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcordenproduccion))
                {
                    model = new TagModel
                    {
                        TagName = "ptcordenproduccion",
                        TagValue = req.LabelTagsOther.TagPtcordenproduccion
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcordenproduccion",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcpesoaproximado))
                {
                    model = new TagModel
                    {
                        TagName = "ptcpesoaproximado",
                        TagValue = req.LabelTagsOther.TagPtcpesoaproximado
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcpesoaproximado",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcqr))
                {
                    model = new TagModel
                    {
                        TagName = "ptcqr",
                        TagValue = req.LabelTagsOther.TagPtcqr
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcqr",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcreferencia))
                {
                    model = new TagModel
                    {
                        TagName = "ptcreferencia",
                        TagValue = req.LabelTagsOther.TagPtcreferencia
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcreferencia",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcresumenrollo))
                {
                    model = new TagModel
                    {
                        TagName = "ptcresumenrollo",
                        TagValue = req.LabelTagsOther.TagPtcresumenrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcresumenrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcrollo))
                {
                    model = new TagModel
                    {
                        TagName = "ptcrollo",
                        TagValue = req.LabelTagsOther.TagPtcrollo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcrollo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtctime))
                {
                    model = new TagModel
                    {
                        TagName = "ptctime",
                        TagValue = req.LabelTagsOther.TagPtctime
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptctime",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcuom))
                {
                    model = new TagModel
                    {
                        TagName = "ptcuom",
                        TagValue = req.LabelTagsOther.TagPtcuom
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcuom",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcuomancho))
                {
                    model = new TagModel
                    {
                        TagName = "ptcuomancho",
                        TagValue = req.LabelTagsOther.TagPtcuomancho
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcuomancho",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }


                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtcuomlg))
                {
                    model = new TagModel
                    {
                        TagName = "ptcuomlg",
                        TagValue = req.LabelTagsOther.TagPtcuomlg
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcuomlg",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

        }

        public async Task CreateOrUpdateMTUProperties(string id, MTURequestMaterialLotProperty[] requestProperties, CancellationToken token)
        {
            logger.LogInformation($"Material tracking unit :: updating/creating properties.");

            //Get properties
            MaterialTrackingUnitProperty[] properties = await SimaticMTUService.GetMTUPropertiesAsync(id, token);

            // Normalize request properties before update
            foreach (MTURequestMaterialLotProperty reqProp in requestProperties)
            {
                ////Parche para generar la propiedad FechaCaducidad
                //if (reqProp.Id.ToUpper().Equals("CADUCIDAD"))
                //{
                //  var extDtStr = reqProp.PropertyValue.ValueString;
                //  var expDt = DateTime.Parse(extDtStr);
                //  requestProperties = requestProperties.Concat(new MTURequestMaterialLotProperty[] { new MTURequestMaterialLotProperty() { Id = "FechaCaducidad", PropertyValue = new MTURequestPropertyValue() { ValueString = expDt.ToString("yyyy-MM-dd HH:mm:ss"), Datatype = "Datetime", UnitOfMeasure = "" } } }).ToArray();
                //}

                // Normalizacion de tipos de datos
                switch (reqProp.PropertyValue.Type.ToLower())
                {
                    case "integer":
                        if (Int32.TryParse(reqProp.PropertyValue.ValueString, out int val))
                        {
                            reqProp.PropertyValue.Type = "Int";
                            reqProp.PropertyValue.ValueString = val.ToString();
                        }
                        else
                        {
                            // can not parse, bad value, save as string may be OK?
                            reqProp.PropertyValue.Type = "String";
                        }
                        break;
                    case "date":
                    case "datetime":
                        if (DateTime.TryParse(reqProp.PropertyValue.ValueString, out DateTime dt))
                        {
                            reqProp.PropertyValue.Type = "Datetime";
                            reqProp.PropertyValue.ValueString = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            // can not parse, bad value, save as string may be OK?
                            reqProp.PropertyValue.Type = "String";
                        }
                        break;
                    default:
                        reqProp.PropertyValue.Type = "String";
                        break;
                }

                // Normalizacion de UoM
                switch (reqProp.PropertyValue.UnitOfMeasure.ToLower())
                {
                    case "n/a":
                    default:
                        reqProp.PropertyValue.UnitOfMeasure = string.Empty;
                        break;
                }
            }

            if ((properties != null && properties.Count() > 0)
              && (requestProperties != null && requestProperties.Count() > 0))
            {
                var updateProp = requestProperties.Where(x => properties.Any(p => p.NId == x.Id)).ToArray();
                if (updateProp.Length > 0)
                {
                    await SimaticMTUService.UpdateMaterialTrackingUnitProperties(id, updateProp, token);
                    logger.LogInformation($"Material tracking unit properties has been updated: Count [{updateProp.Length}].");
                }
            }

            //Create Properties
            MTURequestMaterialLotProperty[] createProp;
            if (properties != null && properties.Count() > 0)
            {
                createProp = requestProperties.Where(x => properties.All(p => p.NId != x.Id)).ToArray();
            }
            else
            {
                createProp = requestProperties.ToArray();
            }

            if (createProp.Length > 0)
            {
                await SimaticMTUService.CreateMaterialTrackingUnitProperties(id, createProp, token);
                logger.LogInformation($"Material tracking unit properties has been created: Count [{createProp.Length}].");
            }
        }

        /// <summary>
        /// Mueve MTU completo O parte de el al destino indicado.
        /// Si el lote no existe, se crea.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task MoveMTUAsync(MTURequest req, CancellationToken ct)
        {
            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

            double quantity = double.Parse(req.Quantity?.QuantityString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

            logger.LogInformation($"Split MTU: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}].");

            //Get Material by ID
            Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, true, ct);

            Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(req.Location.EquipmentID, true, ct);

            MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

            MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

            if (mlot == null)
            {
                var mlotId = await SimaticMTUService.CreateMaterialLot(req.Id, req.MaterialDefinitionID, mat.Description, mat.UoMNId, quantity, ct);
                logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
            }

            if (mtu == null)
            {
                logger.LogInformation($"MTU not found, creating one");

                var mtuId = await SimaticMTUService.CreateMaterialTrackingUnitAsync(req.Id, req.MaterialDefinitionID, mat.Description, eq.NId, mat.UoMNId, quantity, req.MaterialLotProperty, ct);
                logger.LogInformation($"Material Tracking Unit has been created. Id [{mtuId}]");
            }
            else
            {
                logger.LogInformation($"MTU Exists, moving all or part of it");

                // Mover parte del MTU a otra locacion (Split)
                if (quantity < mtu.Quantity.QuantityValue && (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId))
                {
                    // Split MTU
                    await SimaticMTUService.MoveMaterialTrackingUnitAsync(mtu.Id, quantity, eq.NId, ct);
                    // Need to wait until SIMATIC process last execution
                    await Task.Delay(2000);

                    // Find the new created MTU
                    string newMtu = await SimaticMTUService.GetLastCreatedMTUByMaterialLotIdAndEquipmentNIdAsync(mtu.NId, eq.NId, quantity, ct);
                    if (string.IsNullOrEmpty(newMtu))
                    {
                        logger.LogError($"Could not get last created MTU for Lot [{req.Id}] from MTUOperationTracking information.");
                    }
                    else
                    {
                        logger.LogDebug($"Last created MTU found '{newMtu}'");

                        // Refresh MTU information
                        mtu = await SimaticMTUService.GetMTUAsync(newMtu, ct);
                        if (mtu != null)
                        {
                            // Update MTU Details & status
                            await SimaticMTUService.UpdateMaterialTrackingUnitAsync(mtu.Id, mat.Name, mat.Description, req.Status, ct);
                        }
                        else
                        {
                            logger.LogInformation($"Could not find MTU by Guid [{newMtu}], Lot [{req.Id}] for update details!");
                        }
                    }
                }
                else if (quantity == mtu.Quantity.QuantityValue && (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId))
                {
                    // Move entire MTU
                    logger.LogInformation($"Moving entire MTU [{mtu.NId}] from [{mtu.EquipmentNId}], to [{eq.NId}]");
                    await SimaticMTUService.MoveMaterialTrackingUnitToEquipmentAsync(mtu.Id, eq.NId, ct);
                }
                else
                {
                    logger.LogInformation($"MTU Exists, but not any move operation was performed!");
                }

                await CreateOrUpdateMTUProperties(mtu.Id, req.MaterialLotProperty, ct);

            }
        }

        public async Task LotMovementJDE(MTURequest req, CancellationToken ct)
        {
            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

            double quantity = double.Parse(req.Quantity?.QuantityString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

            var movementType = req.MaterialLotProperty.Where(w => w.Id == "TipoMovimiento").FirstOrDefault();

            string type = string.Empty;
            if (movementType != null)
            {
                type = movementType.PropertyValue.ValueString;
            }
            var types = new List<string>() { "R", "E" };
            if (string.IsNullOrWhiteSpace(type) || !types.Contains(type))
            {
                throw new SimaticApiException(HttpStatusCode.BadRequest, $"The Lot [{req.Id}] has invalid TipoMovimiento {type}.");
            }

            logger.LogInformation($"Movinng lot: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}]. Type {type}");


            await SimaticMTUService.HandleLotFromJDE(req, ct);
        }

        public async Task<IList<MTUPropiedadesDefectos>> GetPropertiesDefectos(string Id, CancellationToken ct)
        {
            IList<MTUPropiedadesDefectos> PropiedadesDefectosMTU = new List<MTUPropiedadesDefectos>();
            IList<MaterialTrackingUnitProperty> MaterialTrackingUnitPropertyMTU = new List<MaterialTrackingUnitProperty>();

            var verb = "GetParametersRevisionTrim";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{Id}] WorkOrder [{Id}] for Final Material [{Id}] ...");

            if (!string.IsNullOrEmpty(Id))
            {
                MaterialTrackingUnitPropertyMTU = await SimaticMTUService.GetPropertiesDefectos(Id, ct);

                if (MaterialTrackingUnitPropertyMTU.Count > 0)
                {

                    // Agrupar los defectos por Id
                    var groupedProperties = MaterialTrackingUnitPropertyMTU
                        .GroupBy(p => GetGroupNumber(p.NId));

                    int idCounter = 1; // Contador para asignar valores de Id

                    // Iterar sobre los grupos
                    foreach (var group in groupedProperties)
                    {
                        // Crear un nuevo objeto MTUPropiedadesDefectos para este grupo
                        MTUPropiedadesDefectos mtuProperty = new MTUPropiedadesDefectos();
                        mtuProperty.Id = idCounter;
                        idCounter++;

                        // Asignar los valores correspondientes al objeto MTUPropiedadesDefectos
                        foreach (var property in group)
                        {
                            switch (GetField(property.NId))
                            {
                                case "Codigo":
                                    mtuProperty.Codigo = property.PropertyValue;
                                    break;
                                case "Cantidad":
                                    mtuProperty.Cantidad = property.PropertyValue;
                                    break;
                                case "Descripcion":
                                    mtuProperty.Descripcion = property.PropertyValue;
                                    break;
                                case "Metraje Inicial":
                                    mtuProperty.Metraje_Inicial = property.PropertyValue;
                                    break;
                                case "Metraje Final":
                                    mtuProperty.Metraje_Final = property.PropertyValue;
                                    break;
                                case "C":
                                    mtuProperty.C = property.PropertyValue;
                                    break;
                                case "G":
                                    mtuProperty.G = property.PropertyValue;
                                    break;
                                case "O":
                                    mtuProperty.O = property.PropertyValue;
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Agregar el objeto MTUPropiedadesDefectos a la lista
                        PropiedadesDefectosMTU.Add(mtuProperty);
                    }
                }
            }

            return PropiedadesDefectosMTU;
        }

        // Método para obtener el número al final de 'NId'
        public static string GetGroupNumber(string nId)
        {
            // Dividir el 'NId' en partes usando el espacio como separador
            string[] parts = nId.Split(' ');

            // Devolver el último elemento de la lista como el número de grupo
            return parts.Last();
        }

        // Método para obtener el campo basado en 'NId'
        public static string GetField(string nId)
        {
            // Dividir el 'NId' en partes usando el espacio como separador
            string[] parts = nId.Split(' ');

            // Excluir el primer y último elemento del array
            var middleParts = parts.Skip(1).Take(parts.Length - 2);

            // Concatenar los elementos restantes para formar el campo
            return string.Join(" ", middleParts);
        }

        private static double MetrosCuadradosAMetrosLinealesPorMaterial(double area, string material)
        {
            string[] arrayMaterial = material.Split('.');
            Console.WriteLine("array material: " + string.Join(", ", arrayMaterial));

            string anchoStr = arrayMaterial[arrayMaterial.Length - 1].Split('-')[0];
            double ancho = ConvertirMMaM(anchoStr);
            Console.WriteLine("ancho: " + ancho);

            double longitud = area / ancho;
            return longitud;
        }

        private static double ConvertirMMaM(string mmStr)
        {
            if (double.TryParse(mmStr, out double mm))
            {
                return mm / 1000.0; // Convertir mm a metros
            }
            else
            {
                throw new ArgumentException("El valor proporcionado no es un número válido.");
            }
        }

    }
}

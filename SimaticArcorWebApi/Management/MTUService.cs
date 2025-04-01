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
using SimaticWebApi.Model.Custom.RDL;

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
            if (req.Id.Contains("_LI"))
            {
                req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

                decimal quantity = decimal.Parse(req.Quantity?.QuantityString);

                logger.LogInformation($"Updating lot: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}].");

                //Get Material by ID
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, true, ct);

                if (mat == null)
                {
                    var new_id = req.MaterialDefinitionID.Substring(0, req.MaterialDefinitionID.Length - 1);
                    mat = await SimaticMaterialService.GetMaterialByNIdAsync(new_id, false, true, ct);
                }

                Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(req.Location.EquipmentID, true, ct);

                MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

                string[] parts = req.Id.Split('_');

                MaterialTrackingUnit mtuNormal = await SimaticMTUService.GetMTUAsync(parts[0], ct);

                MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

                if (mlot == null)
                {
                    var mlotId = await SimaticMTUService.CreateMaterialLot(req.Id, mat.NId, mat.Description, mat.UoMNId, Convert.ToDouble(quantity), ct);
                    logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
                }

                if (mtu == null)
                {
                    logger.LogInformation($"MTU not found, creating one");

                    await SimaticMTUService.CreateMaterialTrackingUnitAsync(req.Id, mat.NId, mat.Description, eq.NId, mat.UoMNId, Convert.ToDouble(quantity), req.MaterialLotProperty, ct);

                    logger.LogInformation($"MTU has been created.");

                    //await SimaticMTUService.TriggerNewMaterialTrackingUnitEventAsync(req.Id, ct);

                    // Update info
                    mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);
                    if (req.Status.ToLower() == "asignar")
                    {
                        await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, req.Status, ct);
                    }

                    //logger.LogInformation($"Event triggered.");
                }
                else
                {
                    logger.LogInformation($"Updating MTU");

                    var restQuantity = Convert.ToDouble(quantity) + mtu.Quantity.QuantityValue;

                    if (restQuantity < 0)
                    {
                        restQuantity = 0;
                    }

                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{quantity}] total value [{restQuantity}]");
                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, restQuantity, ct);
                    mtu.Quantity.QuantityValue = restQuantity;

                    logger.LogInformation($"Updating material Lot quantity id [{mtu.NId}] value [{restQuantity}]");
                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, restQuantity, ct);

                    if (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId)
                    {
                        logger.LogInformation($"Moving MTU, id [{mtu.NId}] from [{mtu.EquipmentNId}], to [{eq.NId}]");
                        await SimaticMTUService.MoveMaterialTrackingUnitToEquipmentAsync(mtu.Id, eq.NId, ct);
                    }
                }

                await CreateOrUpdateMTUProperties(mtu.Id, req.MaterialLotProperty, ct);

                if (mtuNormal != null)
                {
                    await CreateOrUpdateMTUProperties(mtuNormal.Id, req.MaterialLotProperty, ct);

                }
            }
            else
            {
                req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

                decimal quantity = decimal.Parse(req.Quantity?.QuantityString);

                logger.LogInformation($"Updating lot: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}].");

                //Get Material by ID
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, false, ct);

                if (mat == null)
                {
                    logger.LogInformation($"El material [{req.MaterialDefinitionID}] no a sido encontrado");
                    var new_id = req.MaterialDefinitionID.Substring(0, req.MaterialDefinitionID.Length - 1);
                    logger.LogInformation($"Buscando por el material [{new_id}]");
                    mat = await SimaticMaterialService.GetMaterialByNIdAsync(new_id, false, true, ct);
                }

                Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(req.Location.EquipmentID, true, ct);

                MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

                MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

                if (mlot == null)
                {
                    var mlotId = await SimaticMTUService.CreateMaterialLot(req.Id, mat.NId, mat.Description, mat.UoMNId, Convert.ToDouble(quantity), ct);
                    logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
                }

                if (mtu == null)
                {
                    logger.LogInformation($"MTU not found, creating one");

                    await SimaticMTUService.CreateMaterialTrackingUnitAsync(req.Id, mat.NId, mat.Description, eq.NId, mat.UoMNId, Convert.ToDouble(quantity), req.MaterialLotProperty, ct);

                    logger.LogInformation($"MTU has been created.");

                    // Update info
                    mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

                    if (req.Status.ToLower() == "asignar")
                    {
                        await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, req.Status, ct);
                    }
                    logger.LogInformation($"find sample if exist for mtu [{req.Id}]");

                    GetSampleMtu mtuSample = await SimaticMTUService.GetMTUSampleAsync(req.Id, ct);
                    if (mtuSample != null)
                    {
                        logger.LogInformation($"found sample if exist for mtu [{mtuSample.SampleNId}]");
                        List<MTURequestMaterialLotProperty> materialLotList = req.MaterialLotProperty?.ToList() ?? new List<MTURequestMaterialLotProperty>();

                        foreach (var item in mtuSample.Properties)
                        {
                            logger.LogInformation($"add [{item.NameProp}] for mtu and value[{item.ValueProp}]");
                            if (item.NameProp != "WorkOrder")
                            {
                                MTURequestMaterialLotProperty ordenar = new MTURequestMaterialLotProperty()
                                {
                                    Id = $"{item.NameProp}",
                                    PropertyValue = new MTURequestPropertyValue()
                                    {
                                        Type = "string",
                                        UnitOfMeasure = "n/a",
                                        ValueString = $"{item.ValueProp}",

                                    }
                                };
                                logger.LogInformation($"ordenar for mtu");

                                materialLotList.Add(ordenar);
                            }

                        }
                        req.MaterialLotProperty = materialLotList.ToArray();

                    }

                }
                else
                {
                    logger.LogInformation($"MTU Exist");

                    MaterialTrackingUnitProperty uniqueId = await SimaticMTUService.GetUniqueId(req.Id, ct);

                    if (uniqueId != null)
                    {
                        var uniqueIdProperty = req.MaterialLotProperty
                            .FirstOrDefault(p => p.Id == "uniqueId");

                        if (uniqueId.PropertyValue == uniqueIdProperty.PropertyValue.ValueString)
                        {
                            logger.LogInformation($"The 'unique Id' of the MTU [{req.Id}] is the same, canceling update");
                            return;
                        }
                    }

                    logger.LogInformation($"Updating MTU");

                    var restQuantity = quantity + Convert.ToDecimal(mtu.Quantity.QuantityValue);

                    if (restQuantity < 0)
                    {
                        restQuantity = 0;
                    }

                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{quantity}] total value [{restQuantity}]");
                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, Convert.ToDouble(restQuantity), ct);
                    mtu.Quantity.QuantityValue = Convert.ToDouble(restQuantity);

                    logger.LogInformation($"Updating material Lot quantity id [{mtu.NId}] value [{restQuantity}]");
                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, Convert.ToDouble(restQuantity), ct);

                    if (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId)
                    {
                        logger.LogInformation($"Moving MTU, id [{mtu.NId}] from [{mtu.EquipmentNId}], to [{eq.NId}]");
                        await SimaticMTUService.MoveMaterialTrackingUnitToEquipmentAsync(mtu.Id, eq.NId, ct);
                    }
                }

                await CreateOrUpdateMTUProperties(mtu.Id, req.MaterialLotProperty, ct);
            }
        }

        public async Task UpdateLotVinilosAsync(MTURequest[] req, CancellationToken ct)
        {
            if (!req[req.Length - 1].Id.EndsWith("E"))
            {
                Material matLot = await SimaticMaterialService.GetMaterialByNIdAsync(req[req.Length - 1].MaterialDefinitionID, false, false, ct);
                if (matLot == null)
                {
                    logger.LogInformation($"El material [{req[req.Length - 1].MaterialDefinitionID}] no a sido encontrado");
                    var new_id = req[req.Length - 1].MaterialDefinitionID.Substring(0, req[req.Length - 1].MaterialDefinitionID.Length - 1);
                    logger.LogInformation($"Buscando por el material [{new_id}]");
                    matLot = await SimaticMaterialService.GetMaterialByNIdAsync(new_id, false, true, ct);
                }
                decimal quantityLot = 0;

                foreach (var mtuItem in req)
                {
                    quantityLot += decimal.Parse(mtuItem.Quantity?.QuantityString);
                }

                MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req[req.Length - 1].Id + "E", ct);

                if (mlot == null)
                {
                    var mlotId = await SimaticMTUService.CreateMaterialLot(req[req.Length - 1].Id + "E", matLot.NId, matLot.Description, matLot.UoMNId, Convert.ToDouble(quantityLot), ct);
                    logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
                }
            }

            foreach (var mtuItem in req)
            {

                mtuItem.Status = string.IsNullOrWhiteSpace(mtuItem.Status) ? "Blanco" : mtuItem.Status;

                decimal quantity = decimal.Parse(mtuItem.Quantity?.QuantityString);
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(mtuItem.MaterialDefinitionID, false, false, ct);

                logger.LogInformation($"Updating lot: Id [{mtuItem.Id}], Status [{mtuItem.Status}], Equipment [{mtuItem.Location.EquipmentID}], Quantity [{quantity}].");

                //Get Material by ID

                if (mat == null)
                {
                    logger.LogInformation($"El material [{mtuItem.MaterialDefinitionID}] no a sido encontrado");
                    var new_id = mtuItem.MaterialDefinitionID.Substring(0, mtuItem.MaterialDefinitionID.Length - 1);
                    logger.LogInformation($"Buscando por el material [{new_id}]");
                    mat = await SimaticMaterialService.GetMaterialByNIdAsync(new_id, false, true, ct);
                }

                Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(mtuItem.Location.EquipmentID, true, ct);

                MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(mtuItem.Id, ct);

                if (mtu == null)
                {
                    logger.LogInformation($"MTU not found, creating one");

                    await SimaticMTUService.CreateMaterialTrackingUnitVinilosAsync(mtuItem.Id, mat.NId, mat.Description, eq.NId, mat.UoMNId, Convert.ToDouble(quantity), req[req.Length - 1].Id.EndsWith("E") ? req[req.Length - 1].Id : req[req.Length - 1].Id + "E", ct);

                    logger.LogInformation($"MTU has been created.");

                    // Update info
                    mtu = await SimaticMTUService.GetMTUAsync(mtuItem.Id, ct);

                    if (mtuItem.Status.ToLower() == "asignar")
                    {
                        await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, mtuItem.Status, ct);
                    }
                    logger.LogInformation($"find sample if exist for mtu [{mtuItem.Id}]");

                    GetSampleMtu mtuSample = await SimaticMTUService.GetMTUSampleAsync(mtuItem.Id, ct);
                    if (mtuSample != null)
                    {
                        logger.LogInformation($"found sample if exist for mtu [{mtuSample.SampleNId}]");
                        List<MTURequestMaterialLotProperty> materialLotList = mtuItem.MaterialLotProperty?.ToList() ?? new List<MTURequestMaterialLotProperty>();

                        foreach (var item in mtuSample.Properties)
                        {
                            logger.LogInformation($"add [{item.NameProp}] for mtu and value[{item.ValueProp}]");
                            if (item.NameProp != "WorkOrder")
                            {
                                MTURequestMaterialLotProperty ordenar = new MTURequestMaterialLotProperty()
                                {
                                    Id = $"{item.NameProp}",
                                    PropertyValue = new MTURequestPropertyValue()
                                    {
                                        Type = "string",
                                        UnitOfMeasure = "n/a",
                                        ValueString = $"{item.ValueProp}",

                                    }
                                };
                                logger.LogInformation($"ordenar for mtu");

                                materialLotList.Add(ordenar);
                            }

                        }
                        mtuItem.MaterialLotProperty = materialLotList.ToArray();

                    }

                }
                else
                {
                    logger.LogInformation($"Updating MTU");

                    var restQuantity = quantity + Convert.ToDecimal(mtu.Quantity.QuantityValue);

                    if (restQuantity < 0)
                    {
                        restQuantity = 0;
                    }

                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{quantity}] total value [{restQuantity}]");
                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, Convert.ToDouble(restQuantity), ct);
                    mtu.Quantity.QuantityValue = Convert.ToDouble(restQuantity);

                    if (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId)
                    {
                        logger.LogInformation($"Moving MTU, id [{mtu.NId}] from [{mtu.EquipmentNId}], to [{eq.NId}]");
                        await SimaticMTUService.MoveMaterialTrackingUnitToEquipmentAsync(mtu.Id, eq.NId, ct);
                    }
                }

                await CreateOrUpdateMTUProperties(mtu.Id, mtuItem.MaterialLotProperty, ct);
            }


        }

        public async Task DescountQuantity(MTUDescount req, CancellationToken ct)
        {

            logger.LogInformation($"Updating lot descount: Id [{req.MtuInfo[0].MtuNId}], Quantity [{req.MtuInfo[0].Quantity}].");

            //Get Material by ID
            MTUAsignedArray[] mtuAsigned = await SimaticMTUService.GetMTUAsigned(req.WorkOrderNId, req.OperationId, ct);

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

                                var restAsigned = item.MtuQuantity - req.MtuInfo[i].Quantity;

                                if (restAsigned <= 0)
                                {
                                    logger.LogInformation($"Quantity [{restAsigned}] procced Unassign MTU [{item.Mtu}] for Work Order [{req.WorkOrderNId}]");
                                    await SimaticMTUService.UnassignMTURequired(item.Id, ct);
                                    await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, "Liberar", ct);

                                }
                                else
                                {
                                    logger.LogInformation($"Updating MTU Asigned quantity id [{item.Mtu}] new value [{restAsigned}]");
                                    await SimaticMTUService.UpdateMTUAsigned(item.Id, restAsigned, ct);
                                }

                                decimal restQuantity = Convert.ToDecimal(mtu.Quantity.QuantityValue) - req.MtuInfo[i].Quantity;

                                if (restQuantity <= 0)
                                {
                                    logger.LogInformation($"Updating MTU and lot quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] [-] new value [{req.MtuInfo[0].Quantity}] = [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, 0, ct);
                                    mtu.Quantity.QuantityValue = Convert.ToDouble(restQuantity);
                                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, 0, ct);

                                }
                                else
                                {

                                    logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] [-] new value [{req.MtuInfo[0].Quantity}] [=] [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, mtu.Quantity.UoMNId, Convert.ToDouble(restQuantity), ct);
                                    mtu.Quantity.QuantityValue = Convert.ToDouble(restQuantity);

                                    logger.LogInformation($"Updating material Lot id [{mtu.NId}] old value [{mlot.Quantity.QuantityValue}] [-] new value [{req.MtuInfo[0].Quantity}] [=] [{restQuantity}]");
                                    await SimaticMTUService.SetMaterialLotQuantity(mlot.Id, mtu.Quantity.UoMNId, Convert.ToDouble(restQuantity), ct);
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
            MTUAsignedArray[] mtuAsigned = await SimaticMTUService.GetMTUAsigned(req.WorkOrderNid, req.OperationId, ct);
            if (mtuAsigned.Length < 1)
            {
                logger.LogInformation($"Mtus No found asigned");

                return;
            }
            foreach (MTUAsigned mtuReq in mtuAsigned[0].MaterialList)
            {
                foreach (var item in req.MTUNid)
                {
                    if (mtuReq.Mtu == item)
                    {

                        logger.LogInformation($"Procced search MTU id");

                        var mtu = await SimaticMTUService.GetMTUAsync(item, ct);
                        var mtuProps = await SimaticMTUService.GetMTUPropertiesAsync(mtu.Id, ct);
                        var lotePadre = "";
                        var ancho = "";
                        var tanda = "";
                        foreach (MaterialTrackingUnitProperty prop in mtuProps)
                        {
                            if (prop.NId == "lote")
                            {
                                lotePadre = prop.PropertyValue;
                            }
                            if (prop.NId == "ancho")
                            {
                                ancho = prop.PropertyValue;
                            }
                            if (prop.NId == "contadorTanda")
                            {
                                var numero = Int32.Parse(prop.PropertyValue);
                                tanda = numero < 10 ? "0" + prop.PropertyValue : prop.PropertyValue;
                            }

                            if (ancho == "")
                            {
                                if (prop.NId.Contains("Medidas"))
                                {
                                    var medidaAncho = prop.PropertyValue.Split("X")[0];
                                    ancho = medidaAncho;
                                }
                            }
                        }
                        MaterialTrackingUnitProperty workOrder = await SimaticMTUService.GetPropertyWorkOrder(item, ct);
                        IList<MTUPropiedadesDefectos> defectos = await GetPropertiesDefectos(item, ct);
                        MaterialTrackingUnitProperty maquinaDeCreacion = await SimaticMTUService.GetMaquinaDeCreacion(item, ct);

                        if (mtu != null)
                        {
                            #region Print Label
                            logger.LogInformation($"Select label unasign");

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
                                    PrintOperation = "Mezclas",
                                    Equipment = req.Equipment
                                };
                            }
                            else if (mtu.MaterialNId.StartsWith("3"))
                            {
                                logger.LogInformation($"label material 3");

                                var date = DateTime.Now;
                                logger.LogInformation($"ancho: [{ancho}]");
                                var largo = mtuReq.MtuQuantity / ConvertirCmAMetros(Convert.ToDecimal(ancho));
                                largo = Math.Truncate(largo);
                                logger.LogInformation($"larga: [{largo}]");
                                label = new PrintModel
                                {
                                    LabelPrinter = req.Printer ?? "PRINT1",
                                    LabelTagsArseg = new ARSEGPrintModel { },
                                    LabelTagsMezclas = new MezclasPrintModel { },
                                    LabelTagsOther = new OtherTagsModel { },
                                    LabelTagsRecubrimiento = new RecubrimientoModel { },
                                    LabelTagsRollos = new RolloPrintModel
                                    {
                                        TagPtacnho = ancho,
                                        TagPtanchodecimales = ancho,
                                        TagPtcantidad = $"{mtuReq.MtuQuantity}",
                                        TagPtdate = date.ToString("dd-MM-yyyy"),
                                        TagPtdatetime = date.ToString("dd-MM-yyyy HH:mm:ss"),
                                        TagPtdescripcion = "",
                                        TagPtdimensiones = $"{ancho}X{largo.ToString()}",
                                        TagPtidmaterial = $"{mtuReq.MaterialNId}",
                                        TagPtlargo = removeDecimalIfZero(largo.ToString()),
                                        TagPtlote = lotePadre,
                                        TagPtmadeincolombia = "",
                                        TagPtoperario = req.Operario,
                                        TagPtordendeproduccion = workOrder != null ? workOrder.PropertyValue : "",
                                        TagPtpesoaproximado = "",
                                        TagPtqr = "",
                                        TagPtreferencia = "",
                                        TagPtresumenrollo = tanda,
                                        TagPtrollo = item,
                                        TagPtuoman = "cm",
                                        TagPtuomlg = "m"
                                    },
                                    LabelTemplate = "productoterminadozpl",
                                    LabelType = "productoTerminadoLbl",
                                    NoOfCopies = "1",
                                    PrintOperation = "Rollos",
                                    Equipment = req.Equipment

                                };
                            }
                            else
                            {
                                var date = DateTime.Now;

                                double ml = MetrosCuadradosAMetrosLinealesPorMaterial(Convert.ToDouble(mtuReq.MtuQuantity), mtuReq.MaterialNId);
                                label = new PrintModel
                                {
                                    LabelTagsRecubrimiento = new RecubrimientoModel
                                    {
                                        TagPpmts2defec = " ", //Total de 'M2' de defecto del rollo
                                        TagPpmtslindefc = " ", //Total de 'M' de defecto del rollo 
                                        TagPpmtspra = $"{Math.Round(ml, 4)}", //Total 'M' de la orden
                                        TagPpmts2pra = $"{mtuReq.MtuQuantity}", // Total 'M2' de la order
                                        TagPpobservaciones = " ", //Observaciones de la orden
                                        TagPpoperario = req.Operario, //Nombre operario
                                        TagPporden = workOrder != null ? workOrder.PropertyValue : "", //Orden de trabajo
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
                                        TagPpmaquina = maquinaDeCreacion != null ? maquinaDeCreacion.PropertyValue : "", //Equipo de trabajo
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
                                    PrintOperation = "Recubrimiento",
                                    Equipment = req.Equipment

                                };

                                if (defectos.Count >= 1)
                                {
                                    foreach (MTUPropiedadesDefectos defect in defectos)
                                    {
                                        label.LabelTagsRecubrimiento.TagPpmts2defec = defect.TotalM2 ?? " ";
                                        label.LabelTagsRecubrimiento.TagPpmtslindefc = defect.TotalM ?? " ";
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

        public async Task<dynamic> GetLotePadreProp(string req, CancellationToken ct)
        {
            logger.LogInformation($"Reciveb MTUNid");

            MaterialTrackingUnitProperty lotePadre = await SimaticMTUService.GetLotePadreProp(req, ct);

            return lotePadre.PropertyValue;
        }

        [Obsolete]
        public async Task TrazabilidadMP(TrazabilidadMPModel req, CancellationToken ct)
        {
            logger.LogInformation($"add Trazabiliti for new MTU [{req.LotId}]");
            foreach (MTUTrazabilidadData item in req.MtuData)
            {
                logger.LogInformation($"add Trazabiliti for MP [{item.MtuNId}]");

                await SimaticMTUService.TrazabilidadMP(req.LotId, req.MaterialNId, req.Quantity, req.UoM, req.User, req.Time, req.EquipmentNId, req.WorkOrderNId, req.WoOperationNId, item.MtuNId, item.Quantity, item.MaterialNId, item.UoM, ct);
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

            decimal quantity = decimal.Parse(req.Quantity?.QuantityString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

            logger.LogInformation($"Split MTU: Id [{req.Id}], Status [{req.Status}], Equipment [{req.Location.EquipmentID}], Quantity [{quantity}].");

            //Get Material by ID
            Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, true, ct);

            Equipment eq = await SimaticEquipmentService.GetEquipmentDataAsync(req.Location.EquipmentID, true, ct);

            MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.Id, ct);

            MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

            if (mlot == null)
            {
                var mlotId = await SimaticMTUService.CreateMaterialLot(req.Id, req.MaterialDefinitionID, mat.Description, mat.UoMNId, Convert.ToDouble(quantity), ct);
                logger.LogInformation($"Material Lot has been created. Id [{mlotId}]");
            }

            if (mtu == null)
            {
                logger.LogInformation($"MTU not found, creating one");

                var mtuId = await SimaticMTUService.CreateMaterialTrackingUnitAsync(req.Id, req.MaterialDefinitionID, mat.Description, eq.NId, mat.UoMNId, Convert.ToDouble(quantity), req.MaterialLotProperty, ct);
                logger.LogInformation($"Material Tracking Unit has been created. Id [{mtuId}]");
            }
            else
            {
                logger.LogInformation($"MTU Exists, moving all or part of it");

                // Mover parte del MTU a otra locacion (Split)
                if (quantity < Convert.ToDecimal(mtu.Quantity.QuantityValue) && (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId))
                {
                    // Split MTU
                    await SimaticMTUService.MoveMaterialTrackingUnitAsync(mtu.Id, Convert.ToDouble(quantity), eq.NId, ct);
                    // Need to wait until SIMATIC process last execution
                    await Task.Delay(2000);

                    // Find the new created MTU
                    string newMtu = await SimaticMTUService.GetLastCreatedMTUByMaterialLotIdAndEquipmentNIdAsync(mtu.NId, eq.NId, Convert.ToDouble(quantity), ct);
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
                else if (quantity == Convert.ToDecimal(mtu.Quantity.QuantityValue) && (!string.IsNullOrEmpty(eq.NId) && eq.NId != mtu.EquipmentNId))
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

                                case "Total M":
                                    mtuProperty.TotalM = property.PropertyValue;
                                    break;
                                case "Total M2":
                                    mtuProperty.TotalM2 = property.PropertyValue;
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

        #region Print Label
        public async Task PrintLabel(PrintModel req, CancellationToken ct)
        {
            if (req.LabelPrinter == "PRINT1")
            {

                logger.LogInformation($"Impresora PRINT1 recibida, Confirmando impresora");

                req.LabelPrinter = await SelectPrinter(req.Equipment, ct) ?? "PRINT1";

            }

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
                        TagValue = removeDecimalIfZero(req.LabelTagsMezclas.TagCantidad.ToString())
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

                if (!string.IsNullOrEmpty(req.LabelTagsMezclas.TagFecha_fab))
                {
                    model = new TagModel
                    {
                        TagName = "Fecha_fab",
                        TagValue = req.LabelTagsMezclas.TagFecha_fab
                    };
                    TagModelList.Add(model);

                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "Fecha_fab",
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
                        TagValue = removeDecimalIfZero(req.LabelTagsRecubrimiento.TagPpmts2pra)
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
                var peso = await SimaticMaterialService.GetMaterialWeigth(mat.NId, ct);
                decimal pesoValue = decimal.Parse(peso.Weigth);

                if (!decimal.TryParse(req.LabelTagsRollos.TagPtcantidad, out decimal tagPtCantidad))
                {
                    throw new InvalidOperationException($"La cantidad obtenida no es válida o no se puede convertir: {req.LabelTagsRollos.TagPtcantidad}");
                }

                decimal calcPeso = pesoValue * tagPtCantidad;
                decimal numeroRedondeadoFinal = Math.Round(calcPeso, 2);

                decimal pesoTotal = numeroRedondeadoFinal;
                logger.LogInformation($"peso del item:[{pesoValue}] kg, cantidad del producido [{tagPtCantidad}]M2, peso calulado: [{pesoTotal}] ");

                req.LabelTagsRollos.TagPtpesoaproximado = pesoTotal.ToString();

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
                        TagValue = removeDecimalIfZero(req.LabelTagsRollos.TagPtcantidad)
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

                if (!string.IsNullOrEmpty(req.LabelTagsRollos.TagPtrevision))
                {
                    model = new TagModel
                    {
                        TagName = "ptrevision",
                        TagValue = req.LabelTagsRollos.TagPtrevision
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptrevision",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "ARSEG")
            {
                var cantidad = req.LabelTagsArseg.TagArcantidadm2.ToString();
                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.LabelTagsArseg.TagAridmaterial, false, true, ct);
                var peso = await SimaticMaterialService.GetMaterialWeigth(mat.NId, ct);
                decimal pesoValue = decimal.Parse(peso.Weigth);


                if (!decimal.TryParse(cantidad, out decimal tagPtCantidad))
                {
                    throw new InvalidOperationException($"La cantidad obtenida no es válida o no se puede convertir: {req.LabelTagsArseg.TagArcantidadm2}");
                }


                decimal calcPeso = pesoValue * tagPtCantidad;
                decimal numeroRedondeadoFinal = Math.Round(calcPeso, 2);

                decimal pesoTotal = numeroRedondeadoFinal;
                logger.LogInformation($"peso del item:[{pesoValue}] kg, cantidad del producido [{tagPtCantidad}]M2, peso calulado: [{pesoTotal}] ");

                req.LabelTagsArseg.TagArpesoaproximado = pesoTotal.ToString();
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
                        TagValue = removeDecimalIfZero(req.LabelTagsArseg.TagArcantidad)
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
                        TagValue = removeDecimalIfZero(req.LabelTagsArseg.TagArcantidadm2)
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

                if (!string.IsNullOrEmpty(req.LabelTagsArseg.TagARrevision))
                {
                    model = new TagModel
                    {
                        TagName = "arrevision",
                        TagValue = req.LabelTagsArseg.TagARrevision
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "arrevision",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }
                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

            if (req.PrintOperation == "Other")
            {
                MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUAsync(req.LabelTagsOther.TagPtcrollo, ct);

                Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(mtu.MaterialNId, false, true, ct);

                var peso = await SimaticMaterialService.GetMaterialWeigth(mat.NId, ct);
                logger.LogInformation($"peso:[{peso.Weigth}]");

                decimal pesoValue = decimal.Parse(peso.Weigth);
                decimal cantidad;
                if (string.IsNullOrEmpty(req.LabelTagsOther.TagPtclargo) || req.LabelTagsOther.TagPtclargo == "undefined")
                {

                    cantidad = decimal.Parse(req.LabelTagsOther.TagPtccantidad);

                }
                else
                {

                    cantidad = decimal.Parse(req.LabelTagsOther.TagPtclargo);

                }

                decimal numeroRedondeado = Math.Round(cantidad, 2);

                logger.LogInformation($"cantidad:[{numeroRedondeado}]");


                decimal calcPeso = pesoValue * numeroRedondeado;
                logger.LogInformation($"peso del item:[{pesoValue}] kg, cantidad del producido [{numeroRedondeado}]M2,peso calulado:[{calcPeso}] ");
                decimal numeroRedondeadoFinal = Math.Round(calcPeso + 3, 2);

                decimal pesoTotal = numeroRedondeadoFinal;
                logger.LogInformation($"peso calulado:[{calcPeso}] + 3 = [{pesoTotal}] ");


                req.LabelTagsOther.TagPtcpesoaproximado = pesoTotal.ToString();

                req.LabelTagsOther.TagPtcdescripcion = mat.Name;
                req.LabelTagsOther.TagPtcidmaterial = mat.NId;

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
                        TagValue = removeDecimalIfZero(req.LabelTagsOther.TagPtccantidad)
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

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtclongitudml))
                {
                    model = new TagModel
                    {
                        TagName = "ptclongitudml",
                        TagValue = req.LabelTagsOther.TagPtclongitudml
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptclongitudml",
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

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtconsecutivo))
                {
                    model = new TagModel
                    {
                        TagName = "ptcconsecutivo",
                        TagValue = req.LabelTagsOther.TagPtconsecutivo
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcconsecutivo",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtestiba))
                {
                    model = new TagModel
                    {
                        TagName = "ptcestiba",
                        TagValue = req.LabelTagsOther.TagPtestiba
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptcestiba",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                //if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtconsecutivoestiba))
                //{
                //    model = new TagModel
                //    {
                //        TagName = "ptconsecutivoestiba",
                //        TagValue = req.LabelTagsOther.TagPtconsecutivoestiba
                //    };
                //    TagModelList.Add(model);
                //}
                //else
                //{
                //    model = new TagModel
                //    {
                //        TagName = "ptconsecutivoestiba",
                //        TagValue = ""
                //    };
                //    TagModelList.Add(model);
                //}

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtclabelcantidadrollos))
                {
                    model = new TagModel
                    {
                        TagName = "ptclabelcantidadrollos ",
                        TagValue = req.LabelTagsOther.TagPtclabelcantidadrollos
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptclabelcantidadrollos",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                if (!string.IsNullOrEmpty(req.LabelTagsOther.TagPtccantidadrollos))
                {
                    model = new TagModel
                    {
                        TagName = "ptccantidadrollos",
                        TagValue = req.LabelTagsOther.TagPtccantidadrollos
                    };
                    TagModelList.Add(model);
                }
                else
                {
                    model = new TagModel
                    {
                        TagName = "ptccantidadrollos",
                        TagValue = ""
                    };
                    TagModelList.Add(model);
                }

                logger.LogInformation($"lista de tags: [{TagModelList.ToArray()}]");

                await SimaticMTUService.PrintLabel(req, TagModelList.ToArray(), ct);
            }

        }

        public async Task<dynamic> GetLabelHistory(ReimpresionFilters req, CancellationToken ct)
        {
            logger.LogInformation($"Get History labels");
            LabelPrintHistory[] history = await SimaticMTUService.LabelHistory(req.DateStart, req.DateEnd, ct);

            List<LabelHistory> historial = new List<LabelHistory>();
            LabelHistory data = new LabelHistory();

            if (history.Length == 0)
            {
                return historial;
            }

            foreach (LabelPrintHistory item in history)
            {

                string lote = string.Empty;
                LabelPrintHistoryTag[] tags = await SimaticMTUService.LabelHistoryTags(item.Id, ct);

                if (!string.IsNullOrEmpty(req.LoteId))
                {
                    tags = tags.Where((i) =>
                        i.TagValue != null &&
                        i.TagValue.Equals(req.LoteId, StringComparison.OrdinalIgnoreCase)).ToArray();

                    if (tags.Length > 0)
                    {
                        foreach (LabelPrintHistoryTag tag in tags)
                        {
                            if (tag.TagName == "lote" || tag.TagName == "pplote" || tag.TagName == "ptrollo" || tag.TagName == "arnrollo" || tag.TagName == "ptcrollo")
                            {
                                lote = tag.TagValue;
                            }


                        }

                        data = new LabelHistory
                        {
                            HistoryId = item.Id,
                            Fecha = item.Timestamp.AddHours(-5),
                            RePrint = item.IsReprint,
                            Lote = lote,
                            Reason = item.Reason

                        };
                        historial.Add(data);

                        if (!string.IsNullOrEmpty(req.LoteId))
                        {
                            historial = historial.Where((i) =>
                                    i.Lote != null &&
                                    i.Lote.Equals(req.LoteId, StringComparison.OrdinalIgnoreCase)).ToList();
                        }

                        historial = historial.OrderByDescending(f => f.Fecha).ToList();

                        return historial;
                    }
                }

                foreach (LabelPrintHistoryTag tag in tags)
                {
                    if (tag.TagName == "lote" || tag.TagName == "pplote" || tag.TagName == "ptrollo" || tag.TagName == "arnrollo" || tag.TagName == "ptcrollo")
                    {
                        lote = tag.TagValue;
                    }


                }

                data = new LabelHistory
                {
                    HistoryId = item.Id,
                    Fecha = item.Timestamp.AddHours(-5),
                    RePrint = item.IsReprint,
                    Lote = lote,
                    Reason = item.Reason

                };
                historial.Add(data);
            }

            if (!string.IsNullOrEmpty(req.LoteId))
            {
                historial = historial.Where((i) =>
                        i.Lote != null &&
                        i.Lote.Equals(req.LoteId, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            historial = historial.OrderByDescending(f => f.Fecha).ToList();

            return historial;
        }

        #endregion

        #region AUXILIAR

        private async Task<string> SelectPrinter(string equipment, CancellationToken ct)
        {
            PrinterListModel[] printers = await SimaticMTUService.GetPrinter(equipment, ct);

            var printer = printers[0].PrinterList[0].Printer;
            logger.LogInformation($"Seleccionando la primer impresora: [{printer}]");

            return printer;
        }

        private static decimal GramosAKilogramos(decimal gramos)
        {
            // 1 kilogramo = 1000 gramos
            decimal kilogramos = gramos / 1000;
            return kilogramos;
        }

        private static string removeDecimalIfZero(string dato)
        {
            string[] decimales = dato.Split(".");
            if (decimales[decimales.Length - 1].StartsWith("00"))
            {
                return decimales[0];
            }
            var valorDecimal = long.Parse(decimales[decimales.Length - 1]);
            if (valorDecimal == 0)
            {
                return decimales[0];
            }
            return dato;

        }

        private static decimal ConvertirCmAMetros(decimal centimetros)
        {
            return centimetros / 100;
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
            Console.WriteLine("longitud: " + longitud);
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

        #endregion

        #region RDL

        public async Task<string> CreateRequirement(CreateRequirementModel req, CancellationToken ct)
        {

            logger.LogInformation($"mtu nId Requirement [{req.REQUIREMENT.SHORT_DESC}].");


            var result = await SimaticMTUService.CreateRequirement(req, ct);

            return result;
        }

        public async Task<string> UpdateRequirement(CreateRequirementModel req, CancellationToken ct)
        {

            logger.LogInformation($"mtu nId Requirement [{req.REQUIREMENT.SHORT_DESC}].");


            var result = await SimaticMTUService.UpdateRequirement(req, ct);

            return result;
        }

        public async Task<string> CreateRequirementCorte(CreateRequirementModelCorte req, CancellationToken ct)
        {
            logger.LogInformation($"Json Requirement Corte [{req}].");
            var result = await SimaticMTUService.CreateRequirementCorte(req, ct);
            return result;
        }

        public async Task<string> UpdateRequirementLength(UpdateLengthModel req, CancellationToken ct)
        {
            logger.LogInformation($"Json Requirement Corte [{req}].");
            var result = await SimaticMTUService.UpdateRequirementLength(req, ct);
            return result;
        }

        public async Task<string> ConsultaRequirement(string Lote, string Nid, CancellationToken ct)
        {
            logger.LogInformation($"Datos[{Lote}],[{Nid}].");
            var result = await SimaticMTUService.ConsultaRequirement(Lote, Nid, ct);
            return result;
        }


        #endregion
    }
}

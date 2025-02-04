using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.BOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticArcorWebApi.Model.Simatic.MTU;
using System.Text.RegularExpressions;
using SimaticWebApi.Model.Custom.Counters;
using SimaticWebApi.Model.Custom.Reproceso;

namespace SimaticArcorWebApi.Management
{
    public class OrderService : IOrderService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<OrderService> logger;

        public ISimaticOrderService SimaticService { get; set; }

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public ISimaticBOMService SimaticBOMService { get; set; }

        public OrderService(ISimaticOrderService simatic, ISimaticMaterialService simaticMaterialService, ISimaticBOMService simaticBOMService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<OrderService>();

            SimaticService = simatic;
            SimaticMaterialService = simaticMaterialService;
            SimaticBOMService = simaticBOMService;
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

        private static double MetrosLinealesAMetrosCuadradosPorMaterial(double longitud, string material)
        {
            string[] arrayMaterial = material.Split('.');
            Console.WriteLine("array material: " + string.Join(", ", arrayMaterial));

            string anchoStr = arrayMaterial[arrayMaterial.Length - 1].Split('-')[0];
            double ancho = ConvertirMMaM(anchoStr);
            Console.WriteLine("ancho: " + ancho);

            double area = longitud * ancho;
            Console.WriteLine("area: " + area);
            return area;
        }

        public async Task<List<MaterialReproceso>> ExtractMaterialsAsync(string input, string equipment, CancellationToken ct)
        {
            // Eliminar la parte "MP:" del string

            if (!string.IsNullOrEmpty(input))
            {

                string materialsPart = input.Substring(3);

                List<MaterialReproceso> materiales = new List<MaterialReproceso>();
                // Dividir el string por el carácter ';' para obtener los materiales
                string[] materialsArray = materialsPart.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string item in materialsArray)
                {
                    string trimmedItem = item.Trim();

                    // Dividir el item por " por " para separar material y cantidad
                    string[] parts = trimmedItem.Split(new string[] { " POR " }, StringSplitOptions.None);

                    Material matData = await SimaticMaterialService.GetMaterialByNIdAsync(parts[0].Trim(), false, false, ct);

                    if (parts.Length == 2)
                    {
                        MaterialReproceso mat = new MaterialReproceso();
                        QuantityReproceso cant = new QuantityReproceso();
                        cant.UoMNId = matData.UoMNId;
                        cant.QuantityValue = !equipment.StartsWith("H") ? (decimal)MetrosLinealesAMetrosCuadradosPorMaterial(Convert.ToDouble(parts[1].Trim()), parts[0]) : Convert.ToDecimal(parts[1].Trim());
                        mat.Material = parts[0].Trim();
                        mat.Quantity = cant;
                        materiales.Add(mat);
                    }
                }

                return materiales;
            }
            return new List<MaterialReproceso>();
        }

        public async Task ProcessNewOrderAsync(ProductionRequest prod, CancellationToken ct)
        {
            bool recreateOrder = false;
            bool changeOrderStatus = false;
            bool isBlockedForUpdates = false;
            bool isReproceso = prod.OrderType == "REPROCESO" ? true : false;

            string orderId = string.Empty;
            string workOrderId = string.Empty;
            string verb = string.Empty;

            List<string> recreateStatus = new List<string>() { "A", "B" }; // First updatable statuses.
            List<string> changeStatus = new List<string>() { "A", "B", "C" }; // Advance from planed to released for production.
            List<string> blockedStatus = new List<string>() { "C", "D", "G", "H" }; // Either input data, or actual Order nor WorkOrder in this states cannot be changed or managed here.
            List<string> cancelableCurrentOrdersStatus = new List<string>() { "A", "B", "D", "C", "Suspendida" }; // Actual Order or WorkOrder status suitable for cancellation.

            logger.LogInformation($"A new Order data is received, analysing Order ID [{prod.Id}] WorkOrder [{prod.WorkOrder}] for Final Material [{prod.AssemblyItem}] ...");

            #region -- Pre-Checks --

            // Blocked for update, will return an exception to NetSuite!
            if (blockedStatus.Contains(prod.Status) && !changeStatus.Contains(prod.Status))
            {
                logger.LogError($"Order [{prod.Id}] - [{prod.WorkOrder}] Incoming status '{prod.Status}' will be not considered for this data processing!");
                throw new Exception($"Order Input status [{prod.Status}] is not valid for farther prosessing!");
            }

            if (prod.Status.ToLower() == "released")
            {
                prod.Status = "A";
            }

            Material material = await SimaticMaterialService.GetMaterialByNIdAsync(prod.AssemblyItem, false, true, ct);
            if (material == null)
            {
                throw new Exception($"Final Material not found. Id [{prod.AssemblyItem}]");
            }

            BillOfMaterials actualBom = await SimaticBOMService.GetBillOfMaterialsByNIdAsync(prod.BomId, false, false, ct); // returns ANY BoM by NId
            if (actualBom == null)
            {
                throw new Exception($"Bill of Materials [{prod.BomId}] not found!");
            }
            else if (!actualBom.IsCurrent)
            {
                throw new Exception($"Bill of Materials [{prod.BomId}] is not CURRENT, can not proceed!");
            }

            BillOfMaterialsItem[] actualBomItems = await SimaticBOMService.GetBillOfMaterialsItemsByIdAsync(actualBom.Id, ct);
            if (actualBomItems == null)
            {
                throw new Exception($"Bill of Materials [{prod.BomId}] has no Items!");
            }

            #endregion

            #region -- Pre-Creation status validations and other operations --

            // Check if Order exists
            WorkOrder currentWorkOrder = null;
            Order currentOrder = await SimaticService.GetOrderByNIdAsync(prod.WorkOrder, false, ct);
            if (currentOrder != null)
            {
                logger.LogInformation($"Order [{prod.Id}] - [{prod.WorkOrder}] already exist. Status [{currentOrder.Status?.StatusNId}]");
                orderId = currentOrder.Id;
                // Check if WorkOrder exists
                currentWorkOrder = await GetWorkOrderAsync(prod, ct);
                if (currentWorkOrder != null)
                {
                    workOrderId = currentWorkOrder.Id;
                }

                if ((recreateStatus.Contains(prod.Status)
                  && recreateStatus.Contains(currentOrder.Status.StatusNId)))

                {
                    // Order & WorkOrder will be recreated completely!
                    recreateOrder = true;
                    logger.LogInformation($"Order ID [{prod.Id}] and WorkOrder [{prod.WorkOrder}] will be recreated completely!");
                }
                else if (
                  (((changeStatus.Contains(prod.Status) && changeStatus.Contains(currentOrder.Status.StatusNId) && prod.Status != currentOrder.Status.StatusNId)
                  || (currentWorkOrder != null && changeStatus.Contains(currentWorkOrder.Status.StatusNId) && prod.Status != currentWorkOrder.Status.StatusNId))
                  || (cancelableCurrentOrdersStatus.Contains(currentOrder.Status.StatusNId) && (currentWorkOrder != null && cancelableCurrentOrdersStatus.Contains(currentWorkOrder.Status.StatusNId))))
                  //&&(!blockedStatus.Contains(currentOrder.Status.StatusNId) && (currentWorkOrder != null && !blockedStatus.Contains(currentWorkOrder.Status.StatusNId)))
                  )
                {
                    /// Rules:
                    /// Incoming status and actual Order status permit status changes, and current status of Order or WorkOrder (if exists) is different from Incoming status.
                    /// Or current Order status is among cancellable.
                    /// And actual status is not within a blocked statuses.

                    // Order & WorkOrder will be change their status!
                    changeOrderStatus = true;
                    logger.LogInformation($"Order ID [{prod.Id}] and WorkOrder [{prod.WorkOrder}] will change their status to '{prod.Status}'!");
                }
                else
                {
                    logger.LogWarning($"Order [{prod.Id}] - [{prod.WorkOrder}] status [{currentOrder.Status.StatusNId}] or WorkOrder status [{currentWorkOrder.Status.StatusNId}] did not permit any changes to be made!");
                    isBlockedForUpdates = true;
                }
            }
            else
            {
                // Order not found, so will create it all.
                logger.LogInformation($"Order [{prod.Id}] - [{prod.WorkOrder}] does not exists, so it will be created!");
                // Check if WorkOrder still exists!
                currentWorkOrder = await GetWorkOrderAsync(prod, ct);
                if (currentWorkOrder != null)
                {
                    await SimaticService.DeleteWorkOrderAsync(currentWorkOrder.Id, ct);
                    logger.LogWarning($"Orphan WorkOrder [{currentWorkOrder.NId}] has been deleted!");
                }
            }

            // Blocked for update, will return an exception to NetSuite!
            if (isBlockedForUpdates && !changeOrderStatus)
            {
                logger.LogWarning($"Order [{prod.Id}] - [{prod.WorkOrder}] :: Aborting any changes on this Order and WorkOrder!");
                //throw new Exception($"Order status do not permit any changes! Status: [{prod.Status}]");
                return;
            }

            // Just change Order & WorkOrder statuses and go away!
            if (changeOrderStatus)
            {
                // Update Order status
                if (currentOrder != null)
                {
                    logger.LogInformation($"El estado recibido es {prod.Status}");
                    verb = string.Empty;
                    switch (prod.Status)
                    {
                        case "A":
                            verb = "Liberar";
                            break;
                        case "B":
                            verb = currentOrder.Status.StatusNId.Equals("A") ? "Liberar" : string.Empty;
                            break;
                        case "C":
                            verb = cancelableCurrentOrdersStatus.Contains(currentOrder.Status.StatusNId) ? "Cancelar" : string.Empty;
                            break;
                        case "CLOSED":
                            verb = "Cancelar";
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(verb))
                    {
                        await SimaticService.SetOrderStatusAsync(currentOrder.Id, verb, ct);
                        logger.LogInformation($"Order [{currentOrder.NId}] status was successfully changed with verb [{verb}]");
                    }
                }

                // Update WorkOrder status
                if (currentWorkOrder != null)
                {
                    verb = string.Empty;
                    switch (prod.Status)
                    {
                        case "A":
                            verb = "Liberar";
                            break;
                        case "B":
                            verb = currentWorkOrder.Status.StatusNId.Equals("A") ? "Liberar" : string.Empty;
                            break;
                        case "C":
                            verb = cancelableCurrentOrdersStatus.Contains(currentOrder.Status.StatusNId) ? "Cancelar" : string.Empty;
                            break;
                        case "CLOSED":
                            verb = "Cancelar";
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(verb))
                    {
                        await SimaticService.SetWorkOrderStatusAsync(currentWorkOrder.Id, verb, ct);
                        logger.LogInformation($"WorkOrder [{currentWorkOrder.NId}] status was successfully changed with verb [{verb}]");
                    }
                }

                return;
            }

            if (recreateOrder && currentWorkOrder != null)
            {
                // Delete actual WorkOrder, Order will be recreated by IOInteroperability itself.
                await SimaticService.DeleteWorkOrderAsync(currentWorkOrder.Id, ct);
                logger.LogInformation($"WorkOrder [{currentWorkOrder.Id}] - [{currentWorkOrder.NId}] has been deleted successfully!");
            }

            #endregion

            #region -- Create / ReCreate Order & WorkOrder --

            // Prepare data for Order creation by IOInteroperability's IOCreateOrder (protected CreateFullOrder)

            #region Order UserFields (aka Properties)

            List<IOOrderUserField> userFieldList = new List<IOOrderUserField> { };
            IOOrderUserField userField = null;

            #region -- Basic properties --

            if (!string.IsNullOrEmpty(prod.Location))
            {
                userField = new IOOrderUserField { NId = "Location", UserFieldValue = prod.Location, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.Department))
            {
                userField = new IOOrderUserField { NId = "Department", UserFieldValue = prod.Department, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.ManufacturingRouting))
            {
                userField = new IOOrderUserField { NId = "Manufacturing Routing", UserFieldValue = prod.ManufacturingRouting, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.ARObservaciones))
            {
                userField = new IOOrderUserField { NId = "AR Observaciones", UserFieldValue = prod.ARObservaciones, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.Customer))
            {
                userField = new IOOrderUserField { NId = "Customer", UserFieldValue = prod.Customer, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.OrdenVenta))
            {
                userField = new IOOrderUserField { NId = "Orden Venta", UserFieldValue = prod.OrdenVenta, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.FechaEntrega))
            {
                userField = new IOOrderUserField { NId = "Fecha Entrega", UserFieldValue = prod.FechaEntrega, UserFieldType = "Datetime" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.InternalItem))
            {
                userField = new IOOrderUserField { NId = "Internal Item", UserFieldValue = prod.InternalItem, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.MadeInColombia))
            {
                userField = new IOOrderUserField { NId = "MadeInColombia", UserFieldValue = prod.MadeInColombia, UserFieldType = "Bool" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.Shelflife))
            {
                userField = new IOOrderUserField { NId = "Vida Util", UserFieldValue = prod.Shelflife, UserFieldType = "String" };
                userFieldList.Add(userField);
            }

            if (!string.IsNullOrEmpty(prod.BomId))
            {
                userField = new IOOrderUserField { NId = "BOM", UserFieldValue = prod.BomId, UserFieldType = "String" };
                userFieldList.Add(userField);
            }


            userField = new IOOrderUserField { NId = "FlagTrim", UserFieldValue = $"{prod.FlagTrim}", UserFieldType = "Bool" };
            userFieldList.Add(userField);


            for (int i = 0; i < prod.ParametersRollosClientes.Length; i++)
            {
                ParametersRollosClientes o = prod.ParametersRollosClientes[i];
                for (int j = 0; j < o.Rollo.Length; j++)
                {
                    userField = new IOOrderUserField { NId = $"PRollo{i + 1} {o.Rollo[j].Name}", UserFieldValue = o.Rollo[j].Value, UserFieldType = "String" };
                    userFieldList.Add(userField);
                }
            }

            #endregion

            #region Order Operations

            List<IOOrderOperation> orderOperationList = new List<IOOrderOperation>();
            IOOrderOperation orderOperation = null;
            foreach (ProductionRequestOperations o in prod.Operations)
            {
                orderOperation = new IOOrderOperation()
                {
                    NId = o.Id,
                    OperationNId = o.Name,
                    Sequence = o.Sequence,
                    AvailableForWorkOrderEnrichment = "True"
                };

                #region EquipmentRequirements

                List<IOOrderOperationEquipmentRequirements> oEquReqList = new List<IOOrderOperationEquipmentRequirements>();
                IOOrderOperationEquipmentRequirements oEquReq = null;

                if (!string.IsNullOrEmpty(o.Asset))
                {
                    if (o.Asset == "02 RIONEGRO")
                    {
                        oEquReq = new IOOrderOperationEquipmentRequirements
                        {
                            EquipmentNId = "02 RIO NEGRO",
                            Sequence = "0"
                        };

                        oEquReqList.Add(oEquReq);

                        return;
                    }

                    oEquReq = new IOOrderOperationEquipmentRequirements
                    {
                        EquipmentNId = o.Asset,
                        Sequence = "0"
                    };

                    oEquReqList.Add(oEquReq);
                }

                int seq = 1;
                if (o.AlternativeAssets != null)
                {
                    foreach (ProductionRequestOperationAlternativeAssets a in o.AlternativeAssets)
                    {
                        if (!string.IsNullOrEmpty(a.Code))
                        {
                            oEquReq = new IOOrderOperationEquipmentRequirements
                            {
                                EquipmentNId = a.Code,
                                Sequence = $"{seq}"
                            };

                            oEquReqList.Add(oEquReq);
                        }

                        seq++;
                    }
                }

                orderOperation.OrderOperationEquipmentRequirements = oEquReqList.ToArray();

                #endregion

                #region MaterialRequirements

                /// *** Ordenes de Corte:
                /// La operacion de Corte va por estandar (BoM Items), pero la operacion de Empaque se comporta de otra manera.
                /// * Empaque: En este caso se deben tomar hojas de la operacion de Corte anterior y conformar Estibas de a 2/5 mil hojas.
                /// Entonces como INPUT y OUTPUT es el mismo material.
                if (!isReproceso)
                {
                    List<IOOrderOperationMaterialRequirements> oMaterialReqList = new List<IOOrderOperationMaterialRequirements>();
                    IOOrderOperationMaterialRequirements oMaterialReq = null;
                    oMaterialReq = new IOOrderOperationMaterialRequirements
                    {
                        MaterialNId = material.NId,
                        Sequence = "0",
                        Direction = "OUTPUT",
                        Quantity = new IOQuantity { QuantityValue = prod.Quantity, UoMNId = material.UoMNId }
                    };

                    oMaterialReqList.Add(oMaterialReq);

                    if (o.Name.ToLower().Equals("empaque"))
                    {
                        oMaterialReq = new IOOrderOperationMaterialRequirements
                        {
                            MaterialNId = material.NId,
                            Sequence = "1",
                            Direction = "INPUT",
                            Quantity = new IOQuantity { QuantityValue = prod.Quantity, UoMNId = material.UoMNId }
                        };

                        oMaterialReqList.Add(oMaterialReq);
                    }
                    else
                    {
                        foreach (BillOfMaterialsItem b in actualBomItems)
                        {
                            decimal bomCalcQty = Convert.ToDecimal(b.Quantity.QuantityValue) * prod.Quantity;

                            oMaterialReq = new IOOrderOperationMaterialRequirements
                            {
                                MaterialNId = b.MaterialNId,
                                Sequence = $"{b.Sequence}",
                                Direction = "INPUT",
                                Quantity = new IOQuantity { QuantityValue = bomCalcQty, UoMNId = b.Quantity.UoMNId }
                            };

                            oMaterialReqList.Add(oMaterialReq);
                        }
                    }

                    orderOperation.OrderOperationMaterialRequirements = oMaterialReqList.ToArray();

                }
                else
                {
                    orderOperation.OrderOperationMaterialRequirements = new List<IOOrderOperationMaterialRequirements>().ToArray();
                }


                #endregion




                #region ParameterRequirements

                List<IOOrderOperationParameterRequirements> oParamterReqList = new List<IOOrderOperationParameterRequirements>();
                IOOrderOperationParameterRequirements oParamReq = null;
                foreach (ProductionRequestOperationParameters p in o.Parameters)
                {
                    if (!string.IsNullOrWhiteSpace(p.Value))
                    {
                        if (p.Name.Contains("trim"))
                        {

                            string[] resultado = Regex.Replace(p.Value, @"(trim|Trim|<br\s*\/?>)", "").Split(new string[] { "//" }, StringSplitOptions.None);
                            Console.WriteLine("Resultado: " + string.Join(", ", resultado));
                            if (resultado.Last() == "")
                            {
                                resultado = resultado.Take(resultado.Length - 1).ToArray();
                            }

                            int contador = 1;
                            foreach (string item in resultado)
                            {
                                var result = item.StartsWith(" ") ? item.TrimStart() : item;

                                oParamReq = new IOOrderOperationParameterRequirements
                                {
                                    ParameterNId = "Trim_" + contador,
                                    ParameterName = "Trim " + contador,
                                    ParameterType = Char.ToUpperInvariant(p.Type[0]) + p.Type.ToLowerInvariant().Substring(1),
                                    ParameterTargetValue = result,

                                    ParameterUoMNId = p.UoM
                                };

                                oParamterReqList.Add(oParamReq);
                                contador++;
                            }
                        }
                        else
                        {
                            oParamReq = new IOOrderOperationParameterRequirements
                            {
                                ParameterNId = p.Name,
                                ParameterName = p.Name,
                                ParameterType = Char.ToUpperInvariant(p.Type[0]) + p.Type.ToLowerInvariant().Substring(1),
                                ParameterTargetValue = p.Value,
                                ParameterUoMNId = p.UoM
                            };

                            oParamterReqList.Add(oParamReq);
                        }


                    }

                    #region Operation Ordenes de Corte
                    Boolean flagRevision = false;
                    if (material.NId.StartsWith("31V"))
                    {
                        flagRevision = false;
                    }
                    else if (material.NId.StartsWith("31") || material.NId.StartsWith("38") || material.NId.StartsWith("32") || material.NId.StartsWith("33") || material.NId.StartsWith("11") || material.NId.StartsWith("13"))
                    {
                        flagRevision = true;
                    }
                    else if (material.NId.StartsWith("34"))
                    {
                        flagRevision = false;
                    }
                    else if (material.NId.StartsWith("37") || material.NId.StartsWith("312"))
                    {
                        flagRevision = true;
                    }

                    if (p.Name.Contains("trim") && flagRevision)
                    {

                        List<IOOrderOperation> orderOperationListCorte = new List<IOOrderOperation>();
                        IOOrderOperation orderOperationCorte = null;

                        orderOperationCorte = new IOOrderOperation()
                        {
                            NId = "455", // Lo podemos asignar
                            OperationNId = "REVISION",
                            Sequence = "999",
                            AvailableForWorkOrderEnrichment = "True"
                        };

                        #region EquipmentRequirements

                        List<IOOrderOperationEquipmentRequirements> oEquReqListCorte = new List<IOOrderOperationEquipmentRequirements>();
                        IOOrderOperationEquipmentRequirements oEquReqCorte = null;

                        oEquReqCorte = new IOOrderOperationEquipmentRequirements
                        {
                            EquipmentNId = prod.Location.StartsWith("02") ? "S01" : "S02",
                            Sequence = "0"
                        };

                        oEquReqListCorte.Add(oEquReqCorte);

                        orderOperationCorte.OrderOperationEquipmentRequirements = oEquReqListCorte.ToArray();

                        #endregion

                        #region MaterialRequirements

                        /// *** Ordenes de Corte:
                        /// La operacion de Corte va por estandar (BoM Items), pero la operacion de Empaque se comporta de otra manera.
                        /// * Empaque: En este caso se deben tomar hojas de la operacion de Corte anterior y conformar Estibas de a 2/5 mil hojas.
                        /// Entonces como INPUT y OUTPUT es el mismo material.

                        List<IOOrderOperationMaterialRequirements> oMaterialReqListCorte = new List<IOOrderOperationMaterialRequirements>();
                        IOOrderOperationMaterialRequirements oMaterialReqCorte = null;
                        oMaterialReqCorte = new IOOrderOperationMaterialRequirements
                        {
                            MaterialNId = material.NId,
                            Sequence = "0",
                            Direction = "OUTPUT",
                            Quantity = new IOQuantity { QuantityValue = prod.Quantity, UoMNId = material.UoMNId }
                        };

                        oMaterialReqListCorte.Add(oMaterialReqCorte);

                        orderOperationCorte.OrderOperationMaterialRequirements = oMaterialReqListCorte.ToArray();

                        #endregion

                        #region ParameterRequirements

                        orderOperationCorte.OrderOperationParameterRequirements = new IOOrderOperationParameterRequirements[0];

                        #endregion

                        orderOperationList.Add(orderOperationCorte);
                    }

                    #endregion
                }

                if (!string.IsNullOrEmpty(o.AnchoUtil))
                {
                    oParamReq = new IOOrderOperationParameterRequirements
                    {
                        ParameterNId = "AnchoUtil",
                        ParameterName = "AnchoUtil",
                        ParameterTargetValue = o.AnchoUtil,
                        ParameterType = "String",
                        ParameterUoMNId = "m2"
                    };

                    oParamterReqList.Add(oParamReq);

                }


                //for (int i = 0; i < o.InventoryDetail.Length; i++)
                //{
                //    ProductionRequestOperationInventoryDetail OID = o.InventoryDetail[i];
                //    oParamReq = new IOOrderOperationParameterRequirements
                //    {
                //        ParameterNId = $"Lote {i}",
                //        ParameterName = $"Lote {i}",
                //        ParameterType = "String",
                //        ParameterTargetValue = OID.Lote,
                //        ParameterUoMNId = "n/a"
                //    };
                //    oParamterReqList.Add(oParamReq);
                //}

                orderOperation.OrderOperationParameterRequirements = oParamterReqList.ToArray();

                #endregion



                orderOperationList.Add(orderOperation);

            }

            #endregion

            // This operation always recreates an Order, this is IOInteroperability standard behaviour.
            orderId = await SimaticService.CreateOrderAsync(prod, material, userFieldList.ToArray(), orderOperationList.ToArray(), ct);

            // Further operations on the Order and WorkOrder
            if (!string.IsNullOrEmpty(orderId))
            {
                logger.LogInformation($"Order [{prod.Id}] - [{prod.WorkOrder}] created/recreated successfully with ID '{orderId}'");

                // Refresh Order information
                currentOrder = await SimaticService.GetOrderByNIdAsync(prod.WorkOrder, false, ct);
                if (currentOrder != null)
                {
                    logger.LogWarning($"Getting Order [{prod.Id}] - [{prod.WorkOrder}] with status [{currentOrder.Status.StatusNId}]");

                    #region Delete Order DEFAULT Operation, if any.

                    // Get DEFAULT Operation from Operations list
                    Operation defOperation = currentOrder.Operations.Where(o => o.OperationNId.ToUpper().Equals("DEFAULT")).FirstOrDefault();
                    if (defOperation != null)
                    {
                        await SimaticService.DeleteOrderOperationAsync(defOperation.Id, ct);
                        logger.LogDebug($"Order [{prod.Id}] WorkOrder [{prod.WorkOrder}] 'DEFAULT' Operation deleted successfully!");
                    }
                    else
                    {
                        logger.LogWarning($"Order [{prod.Id}] WorkOrder [{prod.WorkOrder}] 'DEFAULT' Operation not found, not any Operation will be deleted!");
                    }

                    #endregion

                    // Also check if need to change Order status.
                    #region -- Handle Order status changes --

                    verb = string.Empty;
                    if (currentOrder.Status.StatusNId != prod.Status)
                    {
                        switch (prod.Status)
                        {
                            case "B":
                                verb = currentOrder.Status.StatusNId.Equals("A") ? "Liberar" : string.Empty;
                                break;
                            default:
                                break;
                        }

                        if (!string.IsNullOrEmpty(verb))
                        {
                            await SimaticService.SetOrderStatusAsync(currentOrder.Id, verb, ct);
                            //await SimaticService.SetWorkOrderStatusAsync(currentOrder.Id, verb, ct);
                        }
                    }

                    #endregion

                    #region -- Handle WorkOrder --

                    // Asuming it does not exists, or was deleted for update previously!
                    List<Guid> newWorkOrder = await SimaticService.CreateWorkOrderAsync(currentOrder, material, ct);
                    if (newWorkOrder.FirstOrDefault() != null)
                    {
                        logger.LogInformation($"Successfully created WorkOrder from Order [{prod.Id}] - [{prod.WorkOrder}] with ID: {newWorkOrder.FirstOrDefault()}");

                        // Get WorkOrder actual information.
                        currentWorkOrder = await SimaticService.GetWorkOrderByIdAsync(newWorkOrder.FirstOrDefault().ToString(), false, ct);
                        if (currentWorkOrder != null)
                        {
                            await SimaticService.AddWorkOrderPriority(currentWorkOrder.Id, prod.Priority, ct);
                            verb = string.Empty;
                            if (currentWorkOrder.Status.StatusNId != prod.Status)
                            {
                                switch (prod.Status)
                                {
                                    case "B":
                                        verb = currentWorkOrder.Status.StatusNId.Equals("A") ? "Liberar" : string.Empty;
                                        break;
                                    default:
                                        break;
                                }

                                if (!string.IsNullOrEmpty(verb))
                                {
                                    await SimaticService.SetWorkOrderStatusAsync(currentWorkOrder.Id, verb, ct);

                                    #region -- Set every WorkOrder Operation status to the same as WorkOrder --

                                    IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationAsync(Guid.Parse(currentWorkOrder.Id), ct);
                                    if (woOperations.Count() > 0)
                                    {
                                        logger.LogDebug($"Found {woOperations.Count()} WorkOrder Operations, will update it's status...");

                                        foreach (WorkOrderOperation woOp in woOperations)
                                        {
                                            await SimaticService.SetWorkOrderOperationStatusAsync(woOp.Id, verb, ct);
                                            logger.LogDebug($"WorkOrder Operation {woOp.NId} - [{woOp.Name}] status was successfully updated with a verb '{verb}'!");
                                        }
                                    }
                                    else
                                    {
                                        logger.LogError($"Found NOT any WorkOrder Operation, please check previous logs!");
                                    }




                                    #endregion
                                }
                            }
                        }
                        else
                        {
                            logger.LogError($"WorkOrder was not found by just created WorkOrder ID: [{newWorkOrder}], for Order [{prod.Id}] - [{prod.WorkOrder}]. Check previous errors!");
                            throw new Exception($"Could not find WorkOrder by ID: [{newWorkOrder}], for Order '{prod.WorkOrder}'!");
                        }
                    }
                    else
                    {
                        logger.LogError($"WorkOrder was not created from Order [{prod.Id}] - [{prod.WorkOrder}]. Check previous errors!");
                        throw new Exception($"Could not create WorkOrder from Order '{prod.WorkOrder}'!");
                    }

                    #endregion
                }
                else
                {
                    logger.LogError($"Something went wrong, could not get just created Order [{prod.Id}] by NId [{prod.WorkOrder}]!");
                    throw new Exception($"Could not get just created Order by NId: '{prod.WorkOrder}'!");
                }
            }
            else
            {
                logger.LogError($"Something went wrong, could not get just created Order [{prod.Id}] - [{prod.WorkOrder}], or it was not created at all! Check previous logs.");
                throw new Exception($"Could not create Order [{prod.Id}] - [{prod.WorkOrder}]!");
            }

            #region CreateMaterialsReproceso


            if (isReproceso)
            {
                logger.LogInformation("Es un REPROCESO");
                IList<WorkOrderOperation> woOperationsReproceso = await SimaticService.GetWorkOrderOperationAsync(Guid.Parse(currentWorkOrder.Id), ct);
                IList<WorkOrderOperation> orderOperationsReproceso = await SimaticService.GetOrderOperationAsync(Guid.Parse(currentOrder.Id), ct);
                List<WorkOrderOperationMaterialRequirement> workOrderMaterialesToAddReproceso = new List<WorkOrderOperationMaterialRequirement>();
                List<OrderOperationMaterialRequirement> orderMaterialesToAddReproceso = new List<OrderOperationMaterialRequirement>();
                string workOrderOperationId = "";// = woOperationsReproceso[0].Id; //CUIDADO
                foreach (WorkOrderOperation woOperation in woOperationsReproceso)
                {
                    if (woOperation.OperationNId == prod.Operations[0].Name)
                    {
                        workOrderOperationId = woOperation.Id;
                    }
                }
                string orderOperationId = "";// = orderOperationsReproceso[0].Id;
                foreach (WorkOrderOperation oOperation in orderOperationsReproceso)
                {
                    if (oOperation.OperationNId == prod.Operations[0].Name)
                    {
                        orderOperationId = oOperation.Id;
                    }
                }
                List<MaterialReproceso> materialesReproceso = await ExtractMaterialsAsync(prod.Memo, prod.Operations[0].Asset, ct);
                logger.LogInformation($"Tiene {materialesReproceso.Count} materiales para reproceso");
                WorkOrderOperationMaterialRequirement womaterialToAdd = new WorkOrderOperationMaterialRequirement();
                OrderOperationMaterialRequirement materialToAdd = new OrderOperationMaterialRequirement();
                var cantidad = new QuantityReproceso { QuantityValue = 0, UoMNId = "m2" };
                int sequence = 1;
                foreach (MaterialReproceso m in materialesReproceso)
                {
                    womaterialToAdd = new WorkOrderOperationMaterialRequirement
                    {
                        MaterialNId = m.Material,
                        Quantity = m.Quantity,
                        Sequence = sequence,
                        MaterialRevision = "1",
                        EquipmentNId = "",
                        Direction = "INPUT"

                    };

                    materialToAdd = new OrderOperationMaterialRequirement
                    {
                        MaterialNId = m.Material,
                        Quantity = m.Quantity,
                        Sequence = sequence,
                        MaterialRevision = "1",
                        Direction = "INPUT"

                    };
                    sequence++;
                    workOrderMaterialesToAddReproceso.Add(womaterialToAdd);
                    orderMaterialesToAddReproceso.Add(materialToAdd);

                }
                string response = await SimaticService.AddMaterialReprosesoWorkOrderAsync(workOrderMaterialesToAddReproceso.ToArray(), workOrderOperationId, ct);
                string responseOrder = await SimaticService.AddMaterialReprosesoOrderAsync(orderMaterialesToAddReproceso.ToArray(), orderOperationId, ct);


            }

            #endregion

            //}
            #endregion

            #region -- OLD --
            //try
            //{

            //  //    var fields = (IList<OrderUserField>)await SimaticService.GetOrderUserFields(orderId, ct);

            //  //    await CreateOrUpdateUserField(nameof(prod.ExportNbr), prod.ExportNbr, orderId, fields, ct);

            //  //    await CreateOrUpdateUserField(nameof(prod.Line), prod.Line, orderId, fields, ct);

            //  //    await CreateOrUpdateUserField(nameof(prod.ShipmentOrder), prod.ShipmentOrder, orderId, fields, ct);

            //  //    await CreateOrUpdateUserField("Type", prod.FamilyID, orderId, fields, ct);

            //  //    await CreateOrUpdateUserField("Planta", prod.PlantID, orderId, fields, ct);

            //  //    // TODO: modificar estos dos UserFields para que se guarde lo que se manda por la interfaz
            //  //    await CreateOrUpdateUserField("RoadMapNId", prod.FinalMaterial, orderId, fields, ct);

            //  //    var roadMapType = string.IsNullOrWhiteSpace(prod.RoadMapType) ? "M" : prod.RoadMapType;
            //  //    await CreateOrUpdateUserField("RoadMapType", roadMapType, orderId, fields, ct);

            //  //    if (currentOrder == null)
            //  //    {
            //  //      woId = await SimaticService.CallCreateOrderFromOperationCommand(prod.OrderId,prod.Status, ct);
            //  //    }

            //  //    IList<WorkOrderUserField> userfields = await SimaticService.GetWorkOrderUserFieldsByID(woId, ct);
            //  //    await UpdateWorkOrderUserFields(userfields, prod, ct);

            //  //if (!int.TryParse(prod.Status, out var status))
            //  //      throw new SimaticApiException($"Status field is not a number. Value [{prod.Status}]");

            //  //    if (status == (int)OrderStatusMachine.Active)
            //  //    {
            //  //      /*if (!string.IsNullOrEmpty(orderId))
            //  //        await SimaticService.SetOrderStatus(orderId, "Activate", ct);*/
            //  //	/*
            //  //      if (!string.IsNullOrEmpty(woId))
            //  //        await SimaticService.SetWorkOrderStatus(woId, "Activate", ct);
            //  //		*/
            //  //    }
            //  //    else
            //  //    {
            //  //      //SimaticService.SetOrderStatus(orderId, "Activate", ct);
            //  //    }
            //}
            //catch (Exception e)
            //{
            //  logger.LogError(e, $"Error creating order. Delete Flag [{creatingFlag}]");

            //  if (creatingFlag)
            //  {
            //    //var token = new CancellationToken();

            //    //if (!string.IsNullOrEmpty(woId))
            //    //{
            //    //  logger.LogInformation(e, $"Deleting Work Order [{woId}]");
            //    //  //await SimaticService.DeleteWorkOrder(woId, token);
            //    //}

            //    //if (!string.IsNullOrEmpty(orderId))
            //    //{
            //    //  logger.LogInformation(e, $"Deleting Order [{orderId}]");
            //    //  //await SimaticService.DeleteOrder(orderId, token);
            //    //}

            //    //logger.LogInformation(e, $"Create Order Rollback has been completed");
            //  }

            //  throw;
            //}
            #endregion

            #endregion
        }

        [Obsolete]
        public async Task ProcessNewOrderV2Async(ProductionRequest prod, CancellationToken ct)
        {
            //var creatingFlag = false;

            //logger.LogInformation($"Creating a new ORDER ID [{prod.OrderId}] Final Material [{prod.FinalMaterial}]");

            //Material material = await SimaticMaterialService.GetMaterialByNIdAsync(prod.FinalMaterial, false, true, ct);

            //if (material == null)
            //  throw new Exception($"Final Material not found. Id [{prod.FinalMaterial}]");

            ////RoadMap[] roadMap = await SimaticRoadMapService.GetRoadMapByNIdAsync(prod.FinalMaterial, ct);

            ////if (roadMap.Count() == 0)
            ////    throw new Exception($"RoadMap [{prod.FinalMaterial}] not found.");

            //Order currentOrder = await SimaticService.GetOrderByNIdAsync(prod.OrderId, false, ct);
            //string orderId = string.Empty;
            //string woId = string.Empty;

            //if (currentOrder != null)
            //{
            //  logger.LogWarning($"The order [{prod.OrderId}] already exist. Status [{currentOrder.Status?.StatusNId}]");
            //  throw new SimaticApiException(HttpStatusCode.BadRequest, $"The order [{prod.OrderId}] already exists!");
            //}
            //else
            //{
            //  orderId = await SimaticService.CreateOrderAsync(prod.OrderId, prod.Id, prod.StartTime, prod.EndTime, prod.FamilyID, prod.Status,
            //    prod.FinalMaterial, prod.Quantity, prod.UnitOfMeasure, ct);
            //  creatingFlag = true;
            //}

            //try
            //{

            //  var fields = (IList<OrderUserField>)await SimaticService.GetOrderUserFields(orderId, ct);

            //  //await CreateOrUpdateUserField(nameof(prod.ExportNbr), prod.ExportNbr, orderId, fields, ct);

            //  await CreateOrUpdateUserField(nameof(prod.Line), prod.Line, orderId, fields, ct);

            //  //await CreateOrUpdateUserField(nameof(prod.ShipmentOrder), prod.ShipmentOrder, orderId, fields, ct);

            //  await CreateOrUpdateUserField("Type", prod.FamilyID, orderId, fields, ct);

            //  await CreateOrUpdateUserField("Planta", prod.PlantID, orderId, fields, ct);

            //  //await CreateOrUpdateUserField("RoadMapNId", prod.FinalMaterial, orderId, fields, ct);

            //  //await CreateOrUpdateUserField("RoadMapType", "M", orderId, fields, ct);

            //  //if (!int.TryParse(prod.Status, out var status))
            //  //    throw new SimaticApiException($"Status field is not a number. Value [{prod.Status}]");

            //  //if (status == (int) OrderStatusMachine.Active)
            //  //{
            //  //    await SimaticService.SetOrderStatus(orderId, "Activate", ct);
            //  //}
            //}
            //catch (Exception e)
            //{
            //  logger.LogError(e, $"Error creating order. Delete Flag [{creatingFlag}]");

            //  if (creatingFlag)
            //  {
            //    var token = new CancellationToken();


            //    if (!string.IsNullOrEmpty(orderId))
            //    {
            //      logger.LogInformation(e, $"Deleting Order [{orderId}]");
            //      await SimaticService.DeleteOrder(orderId, token);
            //    }

            //    logger.LogInformation(e, $"Create Order Rollback has been completed");
            //  }

            //  throw;
            //}
        }

        private async Task<dynamic> GetWorkOrderAsync(ProductionRequest prod, CancellationToken ct)
        {
            WorkOrderExtended woExt = await SimaticService.GetWorkOrderExtendedByOrderNIdAsync(prod.WorkOrder, false, ct);

            if (woExt == null)
            {
                logger.LogWarning($"Work Order extended [{prod.WorkOrder}] was not found.");
                return null;
            }

            //logger.LogInformation($"Updating order / work order properties");

            WorkOrder wo = await SimaticService.GetWorkOrderByIdAsync(woExt.WorkOrder_Id, false, ct);

            if (wo == null)
            {
                logger.LogWarning($"Work Order [{prod.WorkOrder}] was not found.");
                return null;
            }

            return wo;
        }

        public async Task<IList<WorkOrderOperationParameterSpecification>> GetParametersRevisionTrim(string orderId, CancellationToken ct)
        {
            var verb = "GetParametersRevisionTrim";
            IList<WorkOrderOperationParameterSpecification> woOperationsParameters = new List<WorkOrderOperationParameterSpecification>();

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{orderId}] WorkOrder [{orderId}] for Final Material [{orderId}] ...");

            if (!string.IsNullOrEmpty(orderId))
            {
                // Revisión si la WorkOrder tiene una operación de revisión creada
                IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationRevision(orderId, ct);

                if (woOperations.Count > 0)
                {
                    // Si existe la Operacion de revisión agregar los paremetros

                    // Revisión si la WorkOrder tiene una operación de revisión creada
                    woOperationsParameters = await SimaticService.GetWorkOrderOperationParameterSpecificationRollos(woOperations.First().Id, ct);

                }

            }

            return woOperationsParameters;
        }

        public async Task<IList<WorkOrderOperationParameterSpecification>> GetParametersRevisionMTU(string orderId, CancellationToken ct)
        {
            var verb = "GetParametersRevisionTrim";
            IList<WorkOrderOperationParameterSpecification> woOperationsParameters = new List<WorkOrderOperationParameterSpecification>();

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{orderId}] WorkOrder [{orderId}] for Final Material [{orderId}] ...");

            if (!string.IsNullOrEmpty(orderId))
            {
                // Revisión si la WorkOrder tiene una operación de revisión creada
                IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationRevision(orderId, ct);

                if (woOperations.Count > 0)
                {
                    // Si existe la Operacion de revisión agregar los paremetros

                    // Revisión si la WorkOrder tiene una operación de revisión creada
                    woOperationsParameters = await SimaticService.GetWorkOrderOperationParameterSpecificationRollosWithOutDeclarar(woOperations.First().Id, ct);

                    int index = 0;
                    foreach (WorkOrderOperationParameterSpecification itemParameter in woOperationsParameters)
                    {
                        woOperationsParameters[index].MaterialTrackingUnitProperty = await SimaticService.GetMaterialTrackingUnitProperty(itemParameter.ParameterToleranceLow, ct);
                        index++;
                    }
                }

            }

            return woOperationsParameters;
        }

        public async Task<IList<MaterialTrackingUnitProperty>> GetParametersMTU(string NId, CancellationToken ct)
        {
            IList<MaterialTrackingUnitProperty> MaterialTrackingUnitProperty = new List<MaterialTrackingUnitProperty>();

            var verb = "GetParametersRevisionTrim";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{NId}] WorkOrder [{NId}] for Final Material [{NId}] ...");

            if (!string.IsNullOrEmpty(NId))
            {
                MaterialTrackingUnitProperty = await SimaticService.GetMaterialTrackingUnitProperty(NId, ct);
            }

            return MaterialTrackingUnitProperty;
        }

        public async Task<WorkOrderOperationParameterSpecification> UpdateCounterTrimCorte(string parameterId, string cantCortes, CancellationToken ct)
        {
            var verb = "UpdateCounterTrimRevision";
            IList<WorkOrderOperationParameterSpecification> parametersSpecification = new List<WorkOrderOperationParameterSpecification>();
            WorkOrderOperationParameterSpecification parameterSpecification = new WorkOrderOperationParameterSpecification();

            logger.LogInformation($"update counter trim [{parameterId}] ...");

            if (!string.IsNullOrEmpty(parameterId))
            {
                // Revisión si la WorkOrder tiene una operación de revisión creada
                parametersSpecification = await SimaticService.GetWorkOrderOperationParameterSpecificationId(parameterId, ct);

                if (parametersSpecification.Count > 0)
                {
                    parameterSpecification = parametersSpecification.First();

                    int valorActual = int.Parse(parameterSpecification.ParameterToleranceLow == null ? "0" : parameterSpecification.ParameterToleranceLow);

                    if (cantCortes != "0" && valorActual == 0)
                    {
                        valorActual = int.Parse(cantCortes);
                    }
                    else
                    {
                        if (cantCortes != "0")
                        {
                            valorActual += int.Parse(cantCortes);
                        }
                        else
                        {
                            valorActual++;
                        }

                    }


                    parameterSpecification.ParameterToleranceLow = valorActual.ToString();

                    UpdateParameterSpecification updateParameter = new UpdateParameterSpecification();

                    updateParameter.Id = parameterSpecification.Id;
                    updateParameter.ParameterValue = parameterSpecification.ParameterTargetValue;
                    updateParameter.ParameterLimitLow = parameterSpecification.ParameterLimitLow;
                    updateParameter.ParameterToleranceLow = parameterSpecification.ParameterToleranceLow;
                    updateParameter.ParameterToleranceHigh = parameterSpecification.ParameterToleranceHigh;
                    updateParameter.ParameterLimitHigh = parameterSpecification.ParameterLimitHigh;
                    updateParameter.ParameterActualValue = parameterSpecification.ParameterActualValue;
                    updateParameter.EquipmentNId = parameterSpecification.EquipmentNId == null ? "" : parameterSpecification.EquipmentNId;
                    updateParameter.ParameterNId = parameterSpecification.ParameterNId;
                    updateParameter.WorkProcessVariableNId = parameterSpecification.WorkProcessVariableNId;
                    updateParameter.TaskParameterNId = parameterSpecification.TaskParameterNId;

                    await SimaticService.UpdateCounterTrimCorteAsync(updateParameter, verb, ct);
                }


            }

            return parameterSpecification;
        }

        public async Task ProcessNewOperationAsync(WorkOrderOperationRevisionRollosRequest prod, CancellationToken ct)
        {
            var verb = "CreateOrdenOperation";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{prod.Id}] WorkOrder [{prod.Id}] for Final Material [{prod.ProcessParameters[0].ParameterNId}] ...");

            if (!string.IsNullOrEmpty(prod.Id))
            {
                // Revisión si la WorkOrder tiene una operación de revisión creada
                IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationRevision(prod.Id, ct);

                if (woOperations.Count > 0)
                {
                    // Si existe la Operacion de revisión agregar los paremetros

                    // Revisión si la WorkOrder tiene una operación de revisión creada
                    IList<WorkOrderOperationParameterSpecification> woOperationsParameters = await SimaticService.GetWorkOrderOperationParameterSpecificationRollos(woOperations.First().Id, ct);
                    int cantidad = woOperationsParameters.Count + 1;
                    int index = 0;
                    prod.Id = woOperations.First().Id;
                    foreach (ProcessParameterRollos itemParameter in prod.ProcessParameters)
                    {
                        prod.ProcessParameters[index].ParameterNId = itemParameter.ParameterNId + "_" + cantidad;


                        cantidad++;
                        index++;
                    }

                    await SimaticService.SetWorkOrderOperationParametersSpecificationAsync(prod, verb, ct);

                }

                return;

                #region --[Obsolete] Set every WorkOrder Operation status to the same as WorkOrder --

                //IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationAsync(Guid.Parse(currentWorkOrder.Id), ct);
                //if (woOperations.Count() > 0)
                //{
                //    logger.LogDebug($"Found {woOperations.Count()} WorkOrder Operations, will update it's status...");

                //    foreach (WorkOrderOperation woOp in woOperations)
                //    {
                //        await SimaticService.SetWorkOrderOperationStatusAsync(woOp.Id, verb, ct);
                //        logger.LogDebug($"WorkOrder Operation {woOp.NId} - [{woOp.Name}] status was successfully updated with a verb '{verb}'!");
                //    }
                //}
                //else
                //{
                //    logger.LogError($"Found NOT any WorkOrder Operation, please check previous logs!");
                //}

                #endregion
            }

        }

        public async Task UpdateChecksRevisionOperationsAsync(WorkOrderOperationRevisionRequest prod, CancellationToken ct)
        {
            var verb = "CreateOrdenOperation";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{prod.Id}] WorkOrder [{prod.Id}] for Final Material [{prod.ProcessParameters[0].ParameterNId}] ...");

            if (!string.IsNullOrEmpty(prod.Id))
            {
                // Revisión si la WorkOrder tiene una operación de revisión creada
                IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationRevision(prod.Id, ct);

                if (woOperations.Count > 0)
                {
                    // Si existe la Operacion de revisión agregar los paremetros

                    // Revisión si la WorkOrder tiene una operación de revisión creada
                    IList<WorkOrderOperationParameterSpecification> woOperationsParameters = await SimaticService.GetWorkOrderOperationParameterSpecificationRollos(woOperations.First().Id, ct);
                    int cantidad = woOperationsParameters.Count + 1;
                    int index = 0;
                    prod.Id = woOperations.First().Id;
                    IList<UpdateParameterSpecification> parametersSpecification = new List<UpdateParameterSpecification>();

                    foreach (ProcessParameter itemParameterEdit in prod.ProcessParameters)
                    {
                        foreach (WorkOrderOperationParameterSpecification itemParameter in woOperationsParameters)
                        {
                            if (itemParameterEdit.ParameterToleranceLow == itemParameter.ParameterToleranceLow)
                            {
                                UpdateParameterSpecification itemParameterSpecification = new UpdateParameterSpecification();
                                itemParameterSpecification.Id = itemParameter.Id;
                                itemParameterSpecification.ParameterValue = itemParameter.ParameterTargetValue;
                                itemParameterSpecification.ParameterLimitLow = itemParameterEdit.ParameterLimitLow;  //Cambio
                                itemParameterSpecification.ParameterToleranceLow = itemParameter.ParameterToleranceLow;
                                itemParameterSpecification.ParameterToleranceHigh = itemParameterEdit.ParameterToleranceHigh;  //Cambio
                                itemParameterSpecification.ParameterLimitHigh = itemParameterEdit.ParameterLimitHigh;
                                itemParameterSpecification.ParameterActualValue = itemParameter.ParameterActualValue;
                                itemParameterSpecification.EquipmentNId = itemParameter.EquipmentNId == null ? "" : itemParameter.EquipmentNId;
                                itemParameterSpecification.ParameterNId = itemParameter.ParameterNId;
                                itemParameterSpecification.WorkProcessVariableNId = itemParameter.WorkProcessVariableNId;
                                itemParameterSpecification.TaskParameterNId = itemParameter.TaskParameterNId;

                                parametersSpecification.Add(itemParameterSpecification);

                            }

                        }

                    }

                    if (parametersSpecification.Count > 0)
                    {
                        await SimaticService.UpdateWorkOrderOperationParameterRequirementAsync(parametersSpecification, verb, ct);
                    }

                }

                return;

                #region --[Obsolete] Set every WorkOrder Operation status to the same as WorkOrder --

                //IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationAsync(Guid.Parse(currentWorkOrder.Id), ct);
                //if (woOperations.Count() > 0)
                //{
                //    logger.LogDebug($"Found {woOperations.Count()} WorkOrder Operations, will update it's status...");

                //    foreach (WorkOrderOperation woOp in woOperations)
                //    {
                //        await SimaticService.SetWorkOrderOperationStatusAsync(woOp.Id, verb, ct);
                //        logger.LogDebug($"WorkOrder Operation {woOp.NId} - [{woOp.Name}] status was successfully updated with a verb '{verb}'!");
                //    }
                //}
                //else
                //{
                //    logger.LogError($"Found NOT any WorkOrder Operation, please check previous logs!");
                //}

                #endregion
            }

        }

        public async Task ChangeStatusAndTimeOrden(StatusAndTimeOrder req, CancellationToken ct)
        {
            //await SimaticService.SetOrderStatusAsync(req.WOId, req.Status, ct);
            logger.LogInformation($"Cambio de estado [{req.Status}]");
            await SimaticService.SetWorkOrderStatusAsync(req.WOId, req.Status, ct);
            Guid WoId = Guid.Parse(req.WOId);
            var wo = await SimaticService.GetWorkOrderExtendedByWorkOrderIdAsync(WoId, false, ct);


            if (req.Status == "Iniciar")
            {
                logger.LogInformation($"Actializando el tiempo de inicio de la Work Order [{req.WOId}] new time [{req.Time}]");

                await SimaticService.ClearWorkOrderActualTime(req.WOId, wo.PlannedStartTime, wo.PlannedEndTime, ct);
                await SimaticService.SetWorkOrderActualTimeStart(req.WOId, req.Time, ct);
            }

            if (req.Status == "Suspender")
            {
                logger.LogInformation($"Actializando el tiempo de fin de la Work Order [{req.WOId}] new time [{req.Time}]");

                await SimaticService.SetWorkOrderActualTimeEnd(req.WOId, req.Time, ct);
            }

            if (req.Status == "Cancelar")
            {
                logger.LogInformation($"Actializando el tiempo de fin de la Work Order [{req.WOId}] new time [{req.Time}]");

                await SimaticService.SetWorkOrderActualTimeEnd(req.WOId, req.Time, ct);

            }

            if (req.Status == "Terminar")
            {
                logger.LogInformation($"Actializando el tiempo de fin de la Work Order [{req.WOId}] new time [{req.Time}]");

                await SimaticService.SetWorkOrderActualTimeEnd(req.WOId, req.Time, ct);

            }

            if (req.Status == "pre-cierre")
            {
                logger.LogInformation($"Actializando el tiempo de fin de la Work Order [{req.WOId}] new time [{req.Time}]");

                await SimaticService.SetWorkOrderActualTimeEnd(req.WOId, req.Time, ct);

            }

        }

        public async Task UpdateWorkOrderCounters(Counters req, CancellationToken ct)
        {
            //await SimaticService.SetOrderStatusAsync(req.WOId, req.Status, ct);
            logger.LogInformation($"Updating Counter for WO [{req.WoID}]");

            logger.LogInformation($"Search WO [{req.WoID}] parameters");
            var WOParams = await SimaticService.GetWorkOrderParametersAsync(req.WoID, ct);

            if (WOParams != null)
            {
                logger.LogInformation($"found WO [{req.WoID}] parameters");
                logger.LogInformation($"Procced or delete parameters");

                foreach (ProcessParameter param in WOParams)
                {
                    if (param.ParameterNId == "cantidadDefectos" || param.ParameterNId == "cantidadRealizada" || param.ParameterNId == "tandasElaboradas")
                    {
                        logger.LogInformation($"Parameter [{param.Id}] remove complete");
                        await SimaticService.DeleteWorkOrderParameter(param.Id, ct);

                    }

                    if (param.ParameterNId == "puntaTrim" || param.ParameterNId == "refill")
                    {
                        foreach (var newparam in req.Parameters)
                        {
                            if (newparam.ParameterNId == "puntaTrim")
                            {
                                newparam.ParameterTargetValue = Convert.ToString(Convert.ToDecimal(newparam.ParameterTargetValue) + Convert.ToDecimal(param.ParameterTargetValue));
                                await SimaticService.DeleteWorkOrderParameter(param.Id, ct);
                            }

                            if (newparam.ParameterNId == "refill")
                            {
                                newparam.ParameterTargetValue = Convert.ToString(Convert.ToDecimal(newparam.ParameterTargetValue) + Convert.ToDecimal(param.ParameterTargetValue));
                                await SimaticService.DeleteWorkOrderParameter(param.Id, ct);
                            }
                        }
                    }

                }
                logger.LogInformation($"Processed to update counters");

                await SimaticService.AddWorkOrderParametersAsync(req.WoID, req.Parameters, ct);

                logger.LogInformation($"Counters updating complete");

            }
            else
            {
                logger.LogInformation($"Processed to update counters");

                await SimaticService.AddWorkOrderParametersAsync(req.WoID, req.Parameters, ct);

                logger.LogInformation($"Counters updating complete");
            }

        }

        public async Task AddMaterialOperationEmpaqueCorteHojasAsync(MaterialOperationEmpaqueCorteHojas prod, CancellationToken ct)
        {
            var verb = "AddMaterialOperationEmpaqueCorteHojasAsync";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{prod.workOrderOperationId}] WorkOrder [{prod.workOrderOperationId}] for Final Material [{prod.workOrderOperationId}] ...");

            if (!string.IsNullOrEmpty(prod.workOrderOperationId))
            {


                // Revisión si la WorkOrder tiene una operación de revisión creada
                IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationAny(prod.workOrderOperationId, "EMPAQUE", ct);

                if (woOperations.Count > 0)
                {
                    // Si existe la Operacion de revisión agregar los paremetros

                    string workOrderOperationId = woOperations.First().Id;

                    foreach (MaterialsQuantity itemMaterial in prod.materialsQuantity)
                    {

                        // Revisión si la WorkOrder tiene una operación de revisión creada
                        IList<MaterialResponse> materialRequirements = await SimaticService.GetWorkOrderOperationMaterialRequirementAndMaterial(workOrderOperationId, itemMaterial.materialFinal, ct);

                        if (materialRequirements.Count == 0)
                        {
                            IList<MaterialRequirement> listMaterialRequirement = new List<MaterialRequirement>();

                            MaterialRequirement itemMaterialRequirement = new MaterialRequirement();
                            itemMaterialRequirement.MaterialNId = itemMaterial.materialFinal;
                            itemMaterialRequirement.MaterialRevision = "1";

                            Quantity quantity = new Quantity();
                            quantity.QuantityValue = itemMaterial.quantity;
                            quantity.UoMNId = "ud";

                            itemMaterialRequirement.Quantity = quantity;
                            itemMaterialRequirement.Sequence = 1;
                            itemMaterialRequirement.Usage = "";
                            itemMaterialRequirement.Direction = "INPUT";
                            itemMaterialRequirement.EquipmentNId = "";
                            itemMaterialRequirement.EquipmentGraphNId = "";

                            listMaterialRequirement.Add(itemMaterialRequirement);

                            await SimaticService.AddWorkOrderOperationMaterialRequirementsToWorkOrderOperation(listMaterialRequirement, workOrderOperationId, verb, ct);
                        }

                    }

                }

                return;

                #region --[Obsolete] Set every WorkOrder Operation status to the same as WorkOrder --

                //IList<WorkOrderOperation> woOperations = await SimaticService.GetWorkOrderOperationAsync(Guid.Parse(currentWorkOrder.Id), ct);
                //if (woOperations.Count() > 0)
                //{
                //    logger.LogDebug($"Found {woOperations.Count()} WorkOrder Operations, will update it's status...");

                //    foreach (WorkOrderOperation woOp in woOperations)
                //    {
                //        await SimaticService.SetWorkOrderOperationStatusAsync(woOp.Id, verb, ct);
                //        logger.LogDebug($"WorkOrder Operation {woOp.NId} - [{woOp.Name}] status was successfully updated with a verb '{verb}'!");
                //    }
                //}
                //else
                //{
                //    logger.LogError($"Found NOT any WorkOrder Operation, please check previous logs!");
                //}

                #endregion
            }

        }

    }
}

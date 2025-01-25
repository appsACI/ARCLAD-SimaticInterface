using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Nancy;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom.BulkReception;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Simatic.Equipment;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MaterialLot;
using SimaticArcorWebApi.Model.Simatic.MTU;

namespace SimaticArcorWebApi.Management
{
    public class BulkReceptionService : IBulkReceptionService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<BulkReceptionService> logger;

        public ISimaticService SimaticService { get; set; }
        public ISimaticMTUService SimaticMTUService { get; set; }

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public ISimaticEquipmentService SimaticEquipmentService { get; set; }
        public IUOMService UomService { get; set; }
        public BulkReceptionService(ISimaticService simaticService, ISimaticMTUService simaticMTUService, ISimaticMaterialService simaticMaterialService, ISimaticEquipmentService simaticEquipment, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<BulkReceptionService>();

            SimaticService = simaticService;
            SimaticMTUService = simaticMTUService;
            SimaticEquipmentService = simaticEquipment;
            SimaticMaterialService = simaticMaterialService;
            UomService = uomService;
        }

        public async Task TruckReceiveProcessingAsync(TruckReceiveRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                // Build up the data to POST.

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        BulkReception_Properties = new
                        {
                            OC = req.OC,
                            Lote = req.Lot,
                            IdCarga = req.IdCarga,
                            Patente = req.Plate.Replace(" ",""),
                            NumeroLinea = req.NroLinea,
                            Location = "",
                            Fecha = req.Datetime,
                            Planta = req.Plant,
                            Material = req.Material,
                            Quantity = new
                            {
                                UoMNId = UomService.GetSimaticUOM(req.UoM),
                                QuantityValue = req.Qty
                            },
                            MTUStatus = req.Estado

                        }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/BulkReceptionApp/odata/CreateBulkReception",
                    new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        //public async Task<IList<BulkReception>> GetBulkReceptionByLotAsync(string lotNId, CancellationToken ct)
        //{
        //    using (var client = new AuditableHttpClient(logger))
        //    {
        //        client.BaseAddress = new Uri(SimaticService.GetUrl());

        //        // We want the response to be JSON.
        //        client.DefaultRequestHeaders.Accept.Clear();
        //        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //        // Add the Authorization header with the AccessToken.
        //        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

        //        HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/BulkReceptionApp/odata/BulkReception?$filter=Lote eq '{lotNId}'", ct);

        //        SimaticServerHelper.CheckFaultResponse(ct, response, logger);

        //        return await response.Content.ReadAsStringAsync()
        //            .ContinueWith(task =>
        //            {
        //                var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

        //                if (result.value.Count >= 1)
        //                    return (IList<BulkReception>)result.value.ToObject<BulkReception[]>();

        //                return null;
        //            }, ct);
        //    }
        //}

        public async Task<IList<BulkReception>> GetBulkReceptionByIdCargaAndLotAndMaterialNameAsync(string lotNId, string idCarga, string materialName, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/BulkReceptionApp/odata/BulkReception?$filter=Lote eq '{lotNId}' and IdCarga eq '{idCarga}' and MaterialName eq '{materialName}'", ct);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return (IList<BulkReception>)result.value.ToObject<BulkReception[]>();

                        return null;
                    }, ct);
            }
        }

        //public async Task UpdateLotAsync(MTURequest req, CancellationToken ct)
        //{
        //    req.Status = string.IsNullOrWhiteSpace(req.Status) ? "Blanco" : req.Status;

        //    double quantity = double.Parse(req.Quantity?.QuantityString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        //    //Get Material by ID
        //    Material mat = await SimaticMaterialService.GetMaterialByNIdAsync(req.MaterialDefinitionID, false, true, ct);

        //    EquipmentConfiguration eq = await SimaticEquipmentService.GetEquipmentByPropertyValue(req.Location.EquipmentID, "COMOS-ID", true, ct);

        //    MaterialLot mlot = await SimaticMTUService.GetMaterialLot(req.Id, ct);

        //    var bulkReceptionList = await GetBulkReceptionByLotAsync(mlot.NId, ct);
        //    if (bulkReceptionList.Count > 0)
        //    {
        //        if (mlot == null)
        //        {
        //            throw new SimaticApiException(HttpStatusCode.BadRequest, $"MaterialLot [{req.Id}] does not exist.");
        //        }

        //        MaterialTrackingUnit mtu = await SimaticMTUService.GetMTUByMaterialLotIdAndEquipmentNIdAsync(mlot.Id, eq.NId, ct);

        //        if (mtu == null)
        //        {
        //            throw new SimaticApiException(HttpStatusCode.BadRequest, $"There is no MTU associated to MaterialLot [{mlot.NId}] in location [{req.Location.EquipmentID}].");
        //        }

        //        logger.LogInformation($"Updating lot: Id [{req.Id}], Status [{req.Status}], Quantity [{quantity}].");

        //        // Set MTU quantity and UoM
        //        logger.LogInformation($"Updating MTU quantity id [{mtu.NId}] old value [{mtu.Quantity.QuantityValue}] new value [{quantity}]");
        //        await SimaticMTUService.SetMaterialTrackingUnitQuantity(mtu.Id, UomService.GetSimaticUOM(req.Quantity?.UnitOfMeasure), quantity, ct);

        //        //if (!string.IsNullOrEmpty(req.Status) && req.Status != mtu.Status.StatusNId)
        //        //{
        //        //    logger.LogInformation($"Updating MTU status, id [{mtu.NId}] old status [{mtu.Status.StatusNId}], new status [{req.Status}]");
        //        //    await SimaticMTUService.SetMaterialTrackingUnitStatus(mtu.Id, req.Status, ct);
        //        //}

        //        await CreateOrUpdateMTUProperties(mtu.Id, req.MaterialLotProperty, ct);

        //        await ConfirmBulkReceptionAsync(bulkReceptionList.First().Id, ct);
        //    }
        //    else
        //    {
        //        throw new SimaticApiException(HttpStatusCode.BadRequest, $"Lot [{req.Id}] is not a valid BulkReception Lot.");
        //    }
        //}

        //public async Task CreateOrUpdateMTUProperties(string id, MTURequestMaterialLotProperty[] requestProperties, CancellationToken token)
        //{
        //    logger.LogInformation($"Material tracking unit updating properties.");

        //    //Get properties
        //    MaterialTrackingUnitProperty[] properties = await SimaticMTUService.GetMTUPropertiesAsync(id, token);
        //    if (properties != null && properties.Length > 0)
        //    {
        //        var updateProp = requestProperties.Where(x => properties.Any(p => p.NId == x.Id)).ToList();
        //        if (updateProp.Count > 0)
        //        {
        //            await SimaticMTUService.UpdateMaterialTrackingUnitProperties(id, updateProp.ToArray(), token);
        //            logger.LogInformation($"Material tracking unit properties has been updated: Count [{updateProp.Count}].");
        //        }
        //        //Create Properties
        //        var createProp = requestProperties.Where(x => properties.All(p => p.NId != x.Id)).ToList();
        //        if (createProp.Count > 0)
        //        {
        //            await SimaticMTUService.CreateMaterialTrackingUnitProperties(id, createProp.ToArray(), token);
        //            logger.LogInformation($"Material tracking unit properties has been created: Count [{createProp.Count}].");
        //        }
        //    }
        //    else
        //    {
        //        //Create Properties
        //        if (requestProperties.Length > 0)
        //        {
        //            await SimaticMTUService.CreateMaterialTrackingUnitProperties(id, requestProperties, token);
        //            logger.LogInformation($"Material tracking unit properties has been created: Count [{requestProperties.Length}].");
        //        }
        //    }
        //}

        public async Task ConfirmBulkReceptionAsync(TruckReceiveRequest req, CancellationToken ct)
        {
            var bulkReceptionList = await GetBulkReceptionByIdCargaAndLotAndMaterialNameAsync(req.Lot, req.IdCarga, req.Material,ct);
            if (bulkReceptionList == null)
            {
                throw new SimaticApiException(HttpStatusCode.BadRequest, $"No BulkReception found for Lot [{req.Lot}], IdCarga [{req.IdCarga}] and Material [{req.Material}]");
            }

            if (bulkReceptionList.Count > 0)
            {
                using (var client = new AuditableHttpClient(logger))
                {
                    client.BaseAddress = new Uri(SimaticService.GetUrl());

                    // We want the response to be JSON.
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Add the Authorization header with the AccessToken.
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                    // Build up the data to POST.

                    var json = JsonConvert.SerializeObject(new
                    {
                        command = new
                        {
                            BulkReceptionId = bulkReceptionList.First().Id,
                            NewQuantity = req.Kilogramos
                        }
                    });

                    var response = await client.PostAsync("sit-svc/application/BulkReceptionApp/odata/ConfirmBulkReception",
                        new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                    SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                    await response.Content.ReadAsStringAsync();
                }
            }
        }
    }
}

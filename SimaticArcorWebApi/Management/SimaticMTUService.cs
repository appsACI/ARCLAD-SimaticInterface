using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Constants;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Custom.PalletReception;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MaterialLot;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.RoadMap;
using SimaticArcorWebApi.Modules.DCMovement;
using SimaticWebApi.Model.Custom.PrintLabel;
using SimaticWebApi.Model.Custom.RDL;

namespace SimaticArcorWebApi.Management
{
    public class SimaticMTUService : ISimaticMTUService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticMTUService> logger;

        #endregion

        public ISimaticService SimaticService { get; set; }

        public IUOMService UomService { get; set; }

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticService"/> class.
        /// </summary>
        public SimaticMTUService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticMTUService>();

            SimaticService = simatic;
            UomService = uomService;
        }
        #endregion

        #region Interface implementation

        public async Task CreatePalletReception(CreatePalletReceptionRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                var doc = req.Properties.FirstOrDefault(i => i.Name == "Documento");
                var line = req.Properties.FirstOrDefault(i => i.Name == "Linea");


                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        STOT_Insert_Properties = new
                        {
                            STOT_Properties_AmountToReturn = req.Quantity.QuantityValue,
                            STOT_Properties_Invoice = req.OT,
                            STOT_Properties_MTUCode = req.Lot,
                            STOT_Properties_ProductCode = req.Material,
                            STOT_Properties_UoMNId = UomService.GetSimaticUOM(req.Quantity.UOMNid),
                            STOT_Properties_Document = doc != null ? doc.Value : string.Empty,
                            STOT_Properties_Line = line != null ? line.Value : string.Empty,
                            STOT_Properties_Status = "",
                            STOT_Properties_Location = ""
                        }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/CustomsApp/odata/STOT_Insert",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        #region MTU

        #region GetMTU
        public async Task<dynamic> GetMTUAsync(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Material/odata/MaterialTrackingUnit?$filter=NId eq '{nid}'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTrackingUnit>)result.value.ToObject<MaterialTrackingUnit[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetMTUSampleAsync(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/MatDeclarationApp/odata/GetMTUSample(function=@x)?@x=%7B%22MTUNId%22%3A%22{nid}%22%7D", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<GetSampleMtu>)result.value.ToObject<GetSampleMtu[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetMTUAsigned(string nid, string opNid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/MatDeclarationApp/odata/OperationMaterials(function=@x)?@x=%7B%22WorkOrderNId%22%3A%22{nid}%22%2C%22OperationId%22%3A%22{opNid}%22%7D", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);


                      //return ((IList<MTUAsignedArray>)result.value.ToObject<MTUAsignedArray[]>());
                      if (result.value.Count >= 1)
                          return ((IList<MTUAsignedArray>)result.value.ToObject<MTUAsignedArray[]>());

                      return null;

                  }, token);
            }
        }

        public async Task<dynamic> GetMTUByMaterialLotIdAndEquipmentNIdAsync(string materialLotId, string equipmentNId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Material/odata/MaterialTrackingUnit?$filter=MaterialLot_Id eq {materialLotId} and EquipmentNId eq '{equipmentNId}'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return ((IList<MaterialTrackingUnit>)result.value.ToObject<MaterialTrackingUnit[]>()).First();

                        return null;
                    }, token);
            }
        }

        /// <summary>
        /// Finds the last MTU created by Lot, Equipment and Quantity
        /// </summary>
        /// <param name="materialLotId"></param>
        /// <param name="equipmentNId"></param>
        /// <param name="quantity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> GetLastCreatedMTUByMaterialLotIdAndEquipmentNIdAsync(string materialLotId, string equipmentNId, double quantity, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/PICore/odata/MTUOperationTracking?$filter=(SourceLotNId eq '{materialLotId}' and DestinationEquipmentNId eq '{equipmentNId}' and SourceQuantity eq {(decimal)quantity})&$orderby=Timestamp desc", token);
                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                        if (result.value.Count >= 1)
                            return result.value[0].DestinationMTUNId;

                        return string.Empty;
                    }, token);
            }
        }

        public async Task<dynamic> GetMTUPropertiesAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit_Id%20eq%20{id}&$expand=MaterialTrackingUnit,SegregationTags", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return result.value.ToObject<MaterialTrackingUnitProperty[]>();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetPropertiesDefectos(string Id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo las Propiedades de Defectos http://arcloud-opc/sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit_Id eq 303e1f5e-941c-ef11-b847-020017035491 and contains(NId, 'Defecto')
                logger.LogInformation($"Start search defects");

                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{Id}' and contains(NId, 'Defecto')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>();

                      return new List<MaterialTrackingUnitProperty>();
                  }, token);
            }
        }

        public async Task<dynamic> GetMaquinaDeCreacion(string Id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));



                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{Id}' and contains(NId, 'MaquinaDecreacion')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetLotePadreProp(string NId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));
                
                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{NId}' and contains(NId, 'lote')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetPropertyWorkOrder(string Id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo las Propiedades de Defectos http://arcloud-opc/sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit_Id eq 303e1f5e-941c-ef11-b847-020017035491 and contains(NId, 'Defecto')

                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{Id}' and contains(NId, 'WorkOrder')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<dynamic> GetPrinter(string equipmentNId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                

                var url = $"sit-svc/Application/MatDeclarationApp/odata/GetPrinter(function=@x)?@x=%7B%22EquipmentNId%22%3A%22{equipmentNId}%22%7D";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<PrinterListModel>)result.value.ToObject<PrinterListModel[]>());

                      return null;
                  }, token);
            }
        }


        #endregion

        #region PostMTU
        public async Task<string> CreateMaterialTrackingUnitAsync(string id, string materialId, string materialDesc, string equipmentId, string uom, double quantity, MTURequestMaterialLotProperty[] propertiesRequest, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Get long number of Material from properties, for MTU Description
                string mtuName = id;
                if (propertiesRequest.Where(x => x.Id.Equals("SegundoItem")).FirstOrDefault() != null)
                {
                    mtuName = propertiesRequest.Where(x => x.Id.Equals("SegundoItem")).FirstOrDefault().PropertyValue.ValueString;
                }

                if (equipmentId == "02 RIONEGRO")
                {
                    equipmentId = "02 RIO NEGRO";
                }

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        NId = id,
                        Name = mtuName,
                        Description = materialDesc,
                        MaterialLotNId = id,
                        MaterialNId = materialId,
                        EquipmentNId = equipmentId,
                        StateMachineNId = "EstadosMTU",
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(uom), QuantityValue = (decimal)quantity },
                    
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialTrackingUnit",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialTrackingUnitId.ToString();
                  }, token);
            }
        }

        public async Task<string> CreateMaterialTrackingUnitVinilosAsync(string id, string materialId, string materialDesc, string equipmentId, string uom, double quantity,string materialLotNId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Get long number of Material from properties, for MTU Description
                string mtuName = id;
         
                if (equipmentId == "02 RIONEGRO")
                {
                    equipmentId = "02 RIO NEGRO";
                }

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        NId = id,
                        Name = mtuName,
                        Description = materialDesc,
                        MaterialLotNId = materialLotNId,
                        MaterialNId = materialId,
                        EquipmentNId = equipmentId,
                        StateMachineNId = "EstadosMTU",
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(uom), QuantityValue = (decimal)quantity },
                     
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialTrackingUnit",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialTrackingUnitId.ToString();
                  }, token);
            }
        }

        public async Task SetMaterialTrackingUnitQuantity(string id, string uoMnId, double quantity, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(uoMnId), QuantityValue = (decimal)quantity }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/SetMaterialTrackingUnitQuantity",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetMaterialTrackingUnitStatus(string id, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Verb = verb
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/SetMaterialTrackingUnitStatus",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Executes PIMaterial_MoveMTU method, basically used for Split.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        /// <param name="equipment"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task MoveMaterialTrackingUnitAsync(string id, double quantity, string equipment, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        MTUId = id,
                        DestinationEquipmentNId = equipment,
                        SourceQuantity = (decimal)quantity,
                        ReasonCode = "JDE Split"
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/PIMaterial_MoveMTU",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Move MTU to Equipment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="equipment"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task MoveMaterialTrackingUnitToEquipmentAsync(string id, string equipment, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        EquipmentNId = equipment
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/MoveMaterialTrackingUnitToEquipment",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task CreateMaterialTrackingUnitProperties(string id, MTURequestMaterialLotProperty[] properties, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        MaterialTrackingUnitId = id,
                        MaterialTrackingUnitProperties = properties.Select(i => new
                        {
                            PropertyNId = i.Id,
                            PropertyType = i.PropertyValue.Type,
                            PropertyUoMNId = i.PropertyValue.UnitOfMeasure,
                            PropertyValue = i.PropertyValue.ValueString
                        }),
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialTrackingUnitProperties",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UnassignMTURequired(string id, CancellationToken token)
        {
            List<string> ids = new List<string>();

            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                ids.Add(id);
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Ids = ids.ToArray()
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/DeleteMaterialRequirementMTU",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region UpdateMTU
        public async Task UpdateMTUAsigned(string id, decimal quantity, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Quantity = quantity

                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateMaterialRequirementMTUV2",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateMaterialTrackingUnitProperties(string id, MTURequestMaterialLotProperty[] properties, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        MaterialTrackingUnitId = id,
                        MaterialTrackingUnitProperties = properties.Select(i => new
                        {
                            PropertyNId = i.Id,
                            PropertyType = i.PropertyValue.Type,
                            PropertyUoMNId = i.PropertyValue.UnitOfMeasure,
                            PropertyValue = i.PropertyValue.ValueString
                        }),
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/UpdateMaterialTrackingUnitProperties",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Updates MTU details
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task UpdateMaterialTrackingUnitAsync(string id, string name, string description, string status, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        StateMachineNId = "EstadosMTU",
                        StatusNId = status
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/UpdateMaterialTrackingUnit",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task DCMovement(DCMovementRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Automation_JDEToSystem_NId = req.Id,
                        Automation_JDEToSystem_FechaIngresoCD = req.MaterialLotProperty.FirstOrDefault(i => i.Id == MTUConstants.MTUConstantsProperties.FechaIngresoCD).PropertyValue.valueString,
                        Automation_JDEToSystem_Item = req.MaterialDefinitionID,
                        Automation_JDEToSystem_PosicionCD = req.MaterialLotProperty.FirstOrDefault(i => i.Id == MTUConstants.MTUConstantsProperties.PosicionCD).PropertyValue.valueString,
                    }
                });

                var response = await client.PostAsync("sit-svc/application/CustomsApp/odata/Automation_JDEToSystem",
                new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task TriggerNewMaterialTrackingUnitEventAsync(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        NId = nid
                    }
                });

                var response = await client.PostAsync("sit-svc/application/MaterialOperationApp/odata/TriggerNewMTUEvent",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                //SimaticServerHelper.CheckFaultResponse(token, response, logger);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Error triggering new MTU event. Response: {response.StatusCode}");
                }
                else
                {
                    logger.LogInformation($"Event triggered.");
                }
                //return await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #endregion

        #region Material Lot

        public async Task<string> CreateMaterialLot(string id, string materialId, string materialDesc, string uom, double quantity, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        NId = id,
                        Name = id,
                        Description = materialDesc,
                        MaterialNId = materialId,
                        UseCurrentRevision = true,
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(uom), QuantityValue = (decimal)quantity },
                        StateMachineNId = "EstadosMTU"
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialLot",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialLotId.ToString();
                  }, token);
            }
        }

        public async Task<dynamic> GetMaterialLot(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Material/odata/MaterialLot?$filter=NId eq '{nid}'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialLot>)result.value.ToObject<MaterialLot[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task SetMaterialLotQuantity(string id, string uoMnId, double quantity, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(uoMnId), QuantityValue = (decimal)quantity }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/SetMaterialLotQuantity",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        #endregion

        #region Trazabilidad
        [Obsolete]
        public async Task<dynamic> TrazabilidadMP(string LotId, string material, decimal quantity, string UoM, string user, DateTime time, string equipment, string WorkOrderNId, string WoOperationNId, string MtuNId, decimal MtuQuantity, string MtuMaterialNId, string MtuUoM, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        User = user,
                        Timestamp = time,
                        OperationType = "MOVE",
                        ReasonCode = "CONSUMO MP",
                        SourceEquipmentNId = equipment,
                        SourceLotNId = MtuNId,
                        SourceMaterialNId = MtuMaterialNId,
                        SourceMaterialRevision = "1",
                        SourceMTUNId = MtuNId,
                        SourceQuantity = MtuQuantity,
                        SourceUoMNId = MtuUoM,
                        SourceWorkOrderNId = WorkOrderNId,
                        SourceWorkOrderOperationNId = WoOperationNId,
                        DestinationEquipmentNId = equipment,
                        DestinationLotNId = LotId,
                        DestinationMaterialNId = material,
                        DestinationMaterialRevision = "1",
                        DestinationMTUNId = LotId,
                        DestinationQuantity = quantity,
                        DestinationUoMNId = UoM,
                        DestinationWorkOrderNId = WorkOrderNId,
                        DestinationWorkOrderOperationNId = WoOperationNId
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialTrackingUnit",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialTrackingUnitId.ToString();
                  }, token);
            }
        }

        #endregion

        #region PrintLabel
        public async Task<dynamic> PrintLabel(PrintModel req, TagModel[] tags, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Get long number of Material from properties, for MTU Description

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new PrinterModel
                    {
                        LabelPrinter = req.LabelPrinter,
                        LabelTemplate = req.LabelTemplate,
                        LabelType = req.LabelType,
                        LabelTags = tags,
                        NoOfCopies = req.NoOfCopies
                    }
                });
                logger.LogInformation($"Label Payload [{json}]");
                var response = await client.PostAsync("sit-svc/Application/Label/odata/PrintLabelAndAddPrintHistory",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result;
                  }, token);
            }
        }

        public async Task<dynamic> LabelHistory(string dateStart, string dateEnd, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/Label/odata/LabelPrintHistory?$filter=(Timestamp ge {dateStart} and Timestamp lt {dateEnd})&$orderby=Timestamp%20desc";



                var response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                 .ContinueWith(task =>
                 {
                     var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                   

                     if (result.value.Count >= 1)
                         return ((IList<LabelPrintHistory>)result.value.ToObject<LabelPrintHistory[]>());

                     return null;
                 }, token);
            }
        }

        public async Task<dynamic> LabelHistoryTags(string historyId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var response = await client.GetAsync($"sit-svc/Application/Label/odata/LabelPrintHistoryTag?$filter=LabelPrintHistory_Id eq {historyId}", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      

                      if (result.value.Count >= 1)
                          return ((IList<LabelPrintHistoryTag>)result.value.ToObject<LabelPrintHistoryTag[]>());

                      return null;
                  }, token);
            }
        }

        #endregion

        public async Task HandleLotFromJDE(MTURequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                var bc = req.MaterialLotProperty.Where(i => i.Id.ToLower() == "barcode").FirstOrDefault();
                var moveType = req.MaterialLotProperty.Where(i => i.Id == "TipoMovimiento").FirstOrDefault();

                var type = moveType.PropertyValue.ValueString;

                decimal quantity = 0;

                decimal.TryParse(req.Quantity.QuantityString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out quantity);

                foreach (var reqProp in req.MaterialLotProperty)
                {
                    if (reqProp.Id.ToUpper().Equals("BARCODE"))
                    {
                        reqProp.Id = "CB";
                    }

                    //Parche para generar la propiedad FechaCaducidad
                    if (reqProp.Id.ToUpper().Equals("CADUCIDAD"))
                    {
                        var extDtStr = reqProp.PropertyValue.ValueString;
                        var expDt = DateTime.Parse(extDtStr);
                        reqProp.Id = "FechaCaducidad";
                        reqProp.PropertyValue.Type = "DateTime";
                        reqProp.PropertyValue.ValueString = expDt.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }

                var properties = req.MaterialLotProperty
                    .Where(w => w.Id != "CB")
                    .Select(s => new
                    {
                        Id = s.Id,
                        ValueString = s.PropertyValue.ValueString,
                        UoM = s.PropertyValue.UnitOfMeasure,
                        DataType = s.PropertyValue.Type
                    }).ToList();

                var data = new
                {
                    LotNId = req.Id,
                    MaterialNId = req.MaterialDefinitionID,
                    Quantity = quantity,
                    UoMNId = req.Quantity.UnitOfMeasure,
                    EquipmentNId = req.Location.EquipmentID,
                    MovementType = type,
                    BarCode = bc.PropertyValue.ValueString,
                    Properties = properties
                };

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = data
                });

                logger.LogInformation(json);
                var response = await client.PostAsync("sit-svc/application/MaterialOperationApp/odata/HandleLotFromJDE",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        #region RDL
        private async Task<dynamic> GetRDLToken(CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Configuration/odata/GetConfigurationKeyList(function=@x)?@x=%7B%22ConfigurationAppDomain_Id%22%3A%222ebd11f7-13c5-ee11-b822-02001708902c%22%7D&$filter=NId%20eq%20'RDnLAuthorization'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<Token>)result.value.ToObject<Token[]>()).First();

                      return null;
                  }, token);
            }
        }

        public async Task<string> CreateRequirement(CreateRequirementModel req, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));


                var data = new { command = req };

                var json = JsonConvert.SerializeObject(data);
                //logger.LogInformation($"json enviado[{json}].");

                var response = await client.PostAsync("sit-svc/application/FBRDL/odata/requirements",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);
                SimaticServerHelper.CheckFaultResponse(token, response, logger);
                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"Respuesta recibida [{result}].");
                      return result.ToString();
                  }, token);
            }
        }

        public async Task<string> UpdateRequirement(CreateRequirementModel req, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));


                var data = new { command = req };

                var json = JsonConvert.SerializeObject(data);
                //logger.LogInformation($"json enviado[{json}].");

                var response = await client.PostAsync("sit-svc/application/FBRDL/odata/UpdateRequirements",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);
                SimaticServerHelper.CheckFaultResponse(token, response, logger);
                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"Respuesta recibida [{result}].");
                      return result.ToString();
                  }, token);
            }
        }

        public async Task<string> CreateRequirementCorte(CreateRequirementModelCorte req, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());
                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var data = new { command = req };
                var json = JsonConvert.SerializeObject(data);
                logger.LogInformation($"json enviado[{json}].");
                var response = await client.PostAsync("sit-svc/application/FBRDL/odata/cuttingsample",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);
                SimaticServerHelper.CheckFaultResponse(token, response, logger);
                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"Respuesta recibida [{result}].");
                      return result.ToString();
                  }, token);
            }
        }

        public async Task<string> UpdateRequirementLength(UpdateLengthModel req, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());
                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var data = new { command = req };

                var json = JsonConvert.SerializeObject(data);

                logger.LogInformation($"json enviado[{json}].");

                var response = await client.PostAsync("sit-svc/application/FBRDL/odata/updatelength", new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"Respuesta recibida [{result}].");
                      return result.ToString();
                  }, token);
            }
        }

        public async Task<string> ConsultaRequirement(string Lote, string Nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                //http://10.10.0.137/OpcenterRDnLGIL/" anterior ip
                client.BaseAddress = new Uri("https://rdl.arclad.com/OpcenterRDnLGIL/");

                var dataToken = await GetRDLToken(token);
                var tk = dataToken.Val.Split(" ")[1];

                var base64Credentials = tk;
                // Agregar el encabezado de autorización
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

                var queryString = $"odata/RndvRq?$filter=DESCRIPTION eq '{Nid}'&$select=RQ,CREATION_DATE";

                var response = await client.GetAsync(queryString);
                response.EnsureSuccessStatusCode();

                // Read the response content as a string
                var content = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<dynamic>(content);
                var values = result["value"];

                // Create a list to store the RQ values
                var rqList = new List<string>();

                // Iterate through the values and extract the RQ values
                foreach (var value in values)
                {
                    rqList.Add(value["RQ"].ToString());
                }
                rqList.Reverse();
                logger.LogInformation($"Lote [{Lote}].");
                string rqDondeSeEncontroLote = "";

                foreach (var rq in rqList)
                {
                    logger.LogInformation($"Rq [{rq}].");

                    var queryString2 = $"odata/RndvRqIi?$filter=RQ eq {rq} AND IC in (186,185,161) AND II in (363)&$select=RQ,II,DSP_TITLE,IIVALUE";
                    response = await client.GetAsync(queryString2);
                    response.EnsureSuccessStatusCode();
                    var content2 = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<dynamic>(content2);
                    logger.LogInformation($"results [{result}].");
                    values = result["value"];
                    logger.LogInformation($"values [{values}].");

                    if (values[0]["IIVALUE"].ToString() == Lote)
                    {
                        rqDondeSeEncontroLote = rq.ToString();
                        logger.LogInformation($"Lote encontrado para RQ: {rq}");
                        break;
                    }

                }

                var queryString3 = $"odata/RndvRqSc?$filter=RQ eq {rqDondeSeEncontroLote}";
                response = await client.GetAsync(queryString3);
                response.EnsureSuccessStatusCode();
                var content3 = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<dynamic>(content3);
                values = result["value"];
                logger.LogInformation($"Rq Response [{values}].");
                var SC = values[0]["SC"];
                logger.LogInformation($"SC Response [{SC}].");
                var queryString4 = $"odata/RndvScIi?$filter=SC eq {SC}&$select=DSP_TITLE,IIVALUE";
                response = await client.GetAsync(queryString4);
                response.EnsureSuccessStatusCode();
                var content4 = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<dynamic>(content4);
                values = result["value"];
                logger.LogInformation($"Response values [{values}].");
                logger.LogInformation($"Response [{values.GetType()}].");

                var requirement = new Requirement();

                foreach (var item in values)
                {
                    string dspTitle = item["DSP_TITLE"]?.ToString().Replace(" ", "_");
                    string value = item["IIVALUE"]?.ToString();

                    switch (dspTitle)
                    {
                        case "PLANTA":
                            requirement.PLANTA = value;
                            break;
                        case "ID_ARCLAD":
                            requirement.ID_ARCLAD = value;
                            break;
                        case "FECHA_DE_FABRICACION":
                            requirement.FECHA_DE_ANALISIS = value;
                            break;
                        case "REFERENCIA":
                            requirement.REFERENCIA = value;
                            break;
                        case "ANCHO_(m)":
                            requirement.ANCHO_M = value;
                            break;
                        case "LONGITUD_(m)":
                            requirement.LONGITUD_M = value;
                            break;
                        case "REFERENCIA_SILICONA":
                            requirement.REFERENCIA_SILICONA = value;
                            break;
                        case "ID_SILICONA":
                            requirement.ID_SILICONA = value;
                            break;
                        case "LOTE_SILICONADO_1":
                            requirement.LOTE_SILICONA_1 = value;
                            break;
                        case "LOTE_SILICONADO_2":
                            requirement.LOTE_SILICONA_2 = value;
                            break;
                        case "LOTE_SILICONADO_3":
                            requirement.LOTE_SILICONA_3 = value;
                            break;
                        case "REFERENCIA_CARA_IMPRESION":
                            requirement.REFERENCIA_CARA_IMPRESION = value;
                            break;
                        case "ID_CARA_IMPRESION":
                            requirement.ID_CARA_IMPRESION = value;
                            break;
                        case "PROVEEDOR_CARA_IMPRESION":
                            requirement.PROVEEDOR_CARA_IMPRESION = value;
                            break;
                        case "LOTE_CARA_IMPRESION_1":
                            requirement.LOTE_CARA_IMPRESION_1 = value;
                            break;
                        case "LOTE_CARA_IMPRESION_2":
                            requirement.LOTE_CARA_IMPRESION_2 = value;
                            break;
                        case "LOTE_CARA_IMPRESION_3":
                            requirement.LOTE_CARA_IMPRESION_3 = value;
                            break;
                        case "LOTE_ADHESIVO_1":
                            requirement.LOTE_ADHESIVO_1 = value;
                            break;
                        case "LOTE_ADHESIVO_2":
                            requirement.LOTE_ADHESIVO_2 = value;
                            break;
                        case "LOTE_ADHESIVO_3":
                            requirement.LOTE_ADHESIVO_3 = value;
                            break;
                        case "REFERENCIA_ADHESIVO":
                            requirement.REFERENCIA_ADHESIVO = value;
                            break;
                        case "ID_ADHESIVO":
                            requirement.ID_ADHESIVO = value;
                            break;
                        case "PROVEEDOR_ADHESIVO":
                            requirement.PROVEEDOR_ADHESIVO = value;
                            break;
                        case "LADO_APLICACION_ADHESIVO":
                            requirement.LADO_APLICACION_ADHESIVO = value;
                            break;
                        case "REFERENCIA_PRIMER":
                            requirement.REFERENCIA_PRIMER = value;
                            break;
                        case "ID_PRIMER":
                            requirement.ID_PRIMER = value;
                            break;
                        case "PROVEEDOR_PRIMER":
                            requirement.PROVEEDOR_PRIMER = value;
                            break;
                        case "LOTE_PRIMER_1":
                            requirement.LOTE_PRIMER_1 = value;
                            break;
                        case "LOTE_PRIMER_2":
                            requirement.LOTE_PRIMER_2 = value;
                            break;
                        case "MAQUINA":
                            requirement.MAQUINA = value;
                            break;
                        case "LADOAPLICACION_SILICONA":
                            requirement.LADOAPLICACION_SILICONA = value;
                            break;
                        case "IMPRESO":
                            requirement.IF_IMPRESO = value;
                            break;
                        case "ARTE":
                            requirement.IF_ARTE = value;
                            break;
                        case "DATOS_CARA_IMPRESION":
                            requirement.IF_DATOSCARAIMPR = value;
                            break;
                        case "DATOS_ADHESIVO":
                            requirement.IF_DATOSADHESIVO = value;
                            break;
                        case "DATOS_SILICONADO":
                            requirement.IF_DATOS_SIL = value;
                            break;
                        case "TURNO":
                            requirement.IF_TURNODESP = value;
                            break;
                        case "ID_LAMINADO-MASA":
                            requirement.ID_LAM_MASA = value;
                            break;
                        case "LOTE_LAMINADO-MASA":
                            requirement.IF_LOTE_LAM_MASA = value;
                            break;
                        case "OP_POLIMERO_EXTERNO":
                            requirement.IF_LOTE_FOTOPOLIMERO = value;
                            break;

                        default:
                            // Caso por defecto si hay DSP_TITLE que no tiene un campo asociado
                            Console.WriteLine($"Campo no mapeado: {dspTitle}");
                            break;
                    }
                }
                requirement.SHORT_DESC = Nid;
                requirement.LOTE = Lote;
                string jsonRequirement = JsonConvert.SerializeObject(requirement, Formatting.Indented);
                logger.LogInformation($"jsonRequirement [{jsonRequirement}].");


                return jsonRequirement.ToString();
            }
        }

        #endregion

        #endregion


    }
}

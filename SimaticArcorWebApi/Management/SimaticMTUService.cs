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

        public async Task<dynamic> GetMTUAsigned(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/MatDeclarationApp/odata/OperationMaterials(function=@x)?@x=%7B%22WorkOrderNId%22%3A%22{nid}%22%7D ", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);


                      return ((IList<MTUAsignedArray>)result.value.ToObject<MTUAsignedArray[]>());


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

                HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit_Id eq {id}", token);

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

                // URL para extraer solo las Propiedades de Defectos http://arcloud-opc/sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit_Id eq 303e1f5e-941c-ef11-b847-020017035491 and contains(NId, 'Defecto')

                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{Id}' and contains(NId, 'MaquinaDecreacion')";

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
                          return (IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>();

                      return new List<MaterialTrackingUnitProperty>();
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
                        //Properties = propertiesRequest.Select(t => new
                        //{
                        //  PropertyNId = t.Id,
                        //  PropertyType = t.PropertyValue.Type,
                        //  //PropertyUoMNId = UomService.GetSimaticUOM(t.UoMNId),
                        //  PropertyValue = t.PropertyValue.ValueString
                        //})
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
        public async Task UpdateMTUAsigned(string id, double quantity, CancellationToken token)
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


        #endregion
    }
}

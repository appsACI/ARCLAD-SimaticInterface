using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.Material;

namespace SimaticArcorWebApi.Management
{
    public class SimaticMaterialService : ISimaticMaterialService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticMaterialService> logger;

        #endregion

        public ISimaticService SimaticService { get; set; }

        public IUOMService UomService { get; set; }

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticMaterialService"/> class.
        /// </summary>
        public SimaticMaterialService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticMaterialService>();

            SimaticService = simatic;
            UomService = uomService;
        }
        #endregion

        #region Interface implementation

        #region GET

        public async Task<dynamic> GetAllMaterialGroupAsync(CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync("sit-svc/application/Material/odata/MaterialGroup", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.value.ToObject<MaterialGroup[]>();
                  }, token);
            }
        }

        public async Task<dynamic> GetAllMaterialGroupByIdAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Material/odata/MaterialGroup?$filter=NId eq '{id}' and IsCurrent eq true", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialGroup>)result.value.ToObject<MaterialGroup[]>()).First();

                      dynamic errorMessage = new { Error = "Material group not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetAllMaterialTemplateAsync(CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync("sit-svc/application/Material/odata/MaterialTemplate", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.value.ToObject<MaterialTemplate[]>();
                  }, token);
            }
        }

        public async Task<dynamic> GetAllMaterialTemplateByIdAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Material/odata/MaterialTemplate?$filter=NId eq '{id}'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTemplate>)result.value.ToObject<MaterialTemplate[]>()).First();

                      dynamic errorMessage = new { Error = "Material template not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetMaterialTemplateByNIdAsync(string id, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/Material/odata/MaterialTemplate?$filter=NId eq '{id}'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<MaterialTemplate>)result.value.ToObject<MaterialTemplate[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = "Material template not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetAllMaterialAsync(CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Material/odata/Material", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.value.ToObject<Material[]>();
                  }, token);
            }
        }

        public async Task<dynamic> GetMaterialByNIdAsync(string id, bool checkCurrent, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/Material/odata/Material?$filter=NId eq '{id}'";

                if (checkCurrent)
                    url += " and IsCurrent eq true";

                url += "&$orderby=Revision desc";

                HttpResponseMessage response = await client.GetAsync(url, token);
                HttpResponseMessage temp_response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<Material>)result.value.ToObject<Material[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = $"El Material [{id}], no esta activo o no a sido encontrado, por favor valida esta informacion." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<MaterialProperties[]> GetMaterialPropertyForMaterialAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Material/odata/MaterialProperty?$filter=Material_Id eq {id}", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.value.ToObject<MaterialProperties[]>();
                  }, token);
            }
        }

        public async Task<dynamic> GetMaterialWeigth(string nid, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/MatDeclarationApp/odata/GetItemWeigth(function=@x)?@x=%7B%22MaterialNId%22%3A%22{nid}%22%7D";

                HttpResponseMessage response = await client.GetAsync(url, token);


                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"Result weigth: [{result}]");

                      if (result.value.Count >= 1)
                          return ((IList<ItemWeight>)result.value.ToObject<ItemWeight[]>()).First();

                      dynamic errorMessage = new { Error = $"Material [{nid}] not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        #endregion

        #region POST

        public async Task<string> CreateMaterialTemplateAsync(string id, string name, string description, string uom, IList<PropertyParameterTypeCreate> properties, CancellationToken token)
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
                        Name = name,
                        Description = description,
                        UoMNId = UomService.GetSimaticUOM(uom),
                        IsDefault = false,
                        Properties = properties
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialTemplate",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialTemplateId.ToString();
                  }, token);
            }
        }

        public async Task<string> CreateMaterialAsync(string id, string name, string description, string uom, string templateNId, string revision, CancellationToken token)
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
                        Name = name,
                        Description = description,
                        TemplateNId = templateNId,
                        Revision = revision,
                        UoMNId = UomService.GetSimaticUOM(uom),
                        UseDefault = false,
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterial",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.MaterialId.ToString();
                  }, token);
            }
        }

        public async Task<string> CreateNewMaterialRevision(string matId, string sourceRevision, string revision, CancellationToken token)
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
                        SourceNId = matId,
                        SourceRevision = sourceRevision,
                        TargetNId = matId,
                        TargetRevision = revision
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CopyMaterialRevision",
                    new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                        return result.MaterialId.ToString();
                    }, token);
            }
        }

        public async Task CreateMaterialProperties(string id, MaterialRequestProperty[] properties, CancellationToken token)
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
                        MaterialId = id,
                        MaterialProperties = properties.Select(i => ConvertMaterialPropertyIntoDynamic(i)),
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/CreateMaterialProperties",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        #endregion

        #region UPDATE
        public async Task UpdateMaterial(string id, string name, string description, string uom, CancellationToken token)
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
                        Name = name,
                        Description = description,
                        UoMNId = UomService.GetSimaticUOM(uom)
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/UpdateMaterial",
                    new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateMaterialProperties(string id, MaterialRequestProperty[] properties, CancellationToken token)
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
                        MaterialId = id,
                        MaterialProperties = properties.Select(i => ConvertMaterialPropertyIntoDynamic(i)),
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/UpdateMaterialProperties",
                new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UnSetMaterialRevisionCurrentAsync(string id, CancellationToken token)
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
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/UnsetMaterialRevisionCurrent",
                    new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetMaterialRevisionCurrentAsync(string id, CancellationToken token)
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
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/SetMaterialRevisionCurrent",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region DELETE
        public async Task DeleteMaterialAsync(string id, CancellationToken token)
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
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/application/Material/odata/DeleteMaterial",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region RDL
        public async Task<dynamic> RDLSpecification(string id, bool isCurrent, CancellationToken token)
        {
            try
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
                            NID = id,
                            IS_CURRENT = isCurrent
                        }

                    });


                    logger.LogInformation($"payload RDL SPECIFICATION[{json}]");

                    logger.LogInformation($"entro al try");

                    var response = await client.PostAsync("sit-svc/application/FBRDL/odata/Specification",
                 new StringContent(json, Encoding.UTF8, "application/json"), token);
                    logger.LogInformation($"enviado a RDL");

                    SimaticServerHelper.CheckFaultResponse(token, response, logger);
                    logger.LogInformation($"leyendo respuesta.");

                    return await response.Content.ReadAsStringAsync()
                      .ContinueWith(task =>
                      {
                          var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                          logger.LogInformation($"RDL result: [{result}].");

                          return result;
                      }, token);


                }
            }
            catch (Exception err)
            {
                logger.LogInformation($"Error rdp esp [{err}]");
                return err;
            }
        }
        #endregion

        #region AUXILIAR

        private object ConvertMaterialPropertyIntoDynamic(MaterialRequestProperty item)
        {
            if (item.Uom == "n/a")
                return new
                {
                    PropertyNId = item.Name,
                    PropertyType = item.Type,
                    PropertyValue = item.Value,
                };

            return new
            {
                PropertyNId = item.Name,
                PropertyType = item.Type,
                PropertyUoMNId = UomService.GetSimaticUOM(item.Uom),
                PropertyValue = item.Value,
            };
        }

        #endregion


        #endregion

    }


}

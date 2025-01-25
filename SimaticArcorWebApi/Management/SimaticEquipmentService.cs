using System;
using System.Collections.Generic;
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
using SimaticArcorWebApi.Model.Simatic.Equipment;

namespace SimaticArcorWebApi.Management
{
  public class SimaticEquipmentService : ISimaticEquipmentService
  {
    #region Fields
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<SimaticEquipmentService> logger;

    #endregion

    public ISimaticService SimaticService { get; set; }

    public IUOMService UomService { get; set; }

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="SimaticEquipmentService"/> class.
    /// </summary>
    public SimaticEquipmentService(ISimaticService simatic, IUOMService uomService)
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticEquipmentService>();

      SimaticService = simatic;
      UomService = uomService;
    }
    #endregion

    #region Interface implementation

    public async Task<dynamic> SetEquipmentPQRAsync(string equId, string pqrNumber, bool throwException, CancellationToken token)
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
            EquipmentNId = equId,
            PQRNumber = pqrNumber
          }
        });

        var response = await client.PostAsync("sit-svc/application/AutomationApp/odata/NSProcessPQRRequest", 
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync();

        //return await response.Content.ReadAsStringAsync()
        //  .ContinueWith(task =>
        //  {
        //    var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
        //    return result.MaterialId.ToString();
        //  }, token);
      }
    }


    /// <summary>
    /// Retrieves the Equipment Properties by Equipment NId.
    /// </summary>
    /// <param name="NId"></param>
    /// <param name="throwException"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<Equipment> GetEquipmentDataAsync(string NId, bool throwException, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Equipment/odata/Equipment?$filter=NId eq '{NId}'&$expand=EquipmentProperties", token);
        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

            if (result.value.Count >= 1)
              return (Equipment)result.value[0].ToObject<Equipment>();

            if (!throwException)
              return null;

            dynamic errorMessage = new { Error = $"Equipment not found. NId [{NId}]" };
            throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
          }, token);
      }
    }

    /// <summary>
    /// Retrieves the Equipment Configuration Properties by Equipment NId.
    /// </summary>
    /// <param name="NId"></param>
    /// <param name="throwException"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<IList<EquipmentConfigurationProperty>> GetEquipmentConfigurationPropertyDataAsync(string NId, bool throwException, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // http://localhost/sit-svc/Application/Equipment/odata/EquipmentConfiguration?$filter=contains(NId,'k09')&$top=20&$skip=0&$orderby=NId asc
        // EquipmentConfigurationProperty?$filter=EquipmentConfiguration/NId%20eq%20'K09'&$expand=EquipmentConfiguration
        HttpResponseMessage response = await client.GetAsync($"sit-svc/application/Equipment/odata/EquipmentConfigurationProperty?$filter=EquipmentConfiguration/NId eq '{NId}'&$expand=EquipmentConfiguration", token);
        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

            if (result.value.Count >= 1)
              return (IList<EquipmentConfigurationProperty>)result.value.ToObject<EquipmentConfigurationProperty[]>();

            if (!throwException)
              return null;

            dynamic errorMessage = new { Error = $"Equipment not found. NId [{NId}]" };
            throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
          }, token);
      }
    }

    [Obsolete]
    public async Task<dynamic> GetEquipmentByPropertyValueAsync(string propertyValue, string propertyName, bool throwException, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        HttpResponseMessage response = await client.GetAsync(
            $"sit-svc/application/Equipment/odata/EquipmentConfigurationProperty?$filter=PropertyValue eq '{propertyValue}' and  NId eq '{propertyName}' &$expand=EquipmentConfiguration&$select=EquipmentConfiguration", token);
        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

              if (result.value.Count >= 1)
                return (EquipmentConfiguration)result.value[0].EquipmentConfiguration.ToObject<EquipmentConfiguration>();

              if (!throwException)
                return null;

              dynamic errorMessage = new { Error = $"Equipment not found with property [{propertyName}] = [{propertyValue}]" };
              throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            }, token);
      }
    }

    public async Task<dynamic> AddEquipmentConfigurationPropertiesAsync(Guid equConfigId, List<EquipmentConfigurationPropertyType> equProps, CancellationToken token)
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
        // {"command":{"Id":"2b3e0be0-fd88-ec11-b828-020017035491","Name":"Cortadora Kampf 9","Description":"Cortadora Kampf 9 KAMPF / AUTOSLIT III 17/06","Properties":[{"NId":"Test","PropertyType":"Datetime","Operational":true}]}}
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = equConfigId,
            Properties = equProps
          }
        });

        var response = await client.PostAsync("sit-svc/Application/Equipment/odata/UpdateEquipmentConfiguration",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync();
      }
    }

    public async Task<dynamic> SetEquipmentPropertyValuesAsync(Guid equPropId, string qeuPropValue, CancellationToken token)
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
            EquipmentPropertyId = equPropId,
            EquipmentPropertyValue = qeuPropValue
          }
        });

        var response = await client.PostAsync("sit-svc/Application/Equipment/odata/SetEquipmentPropertyValue",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync();
      }
    }


    #endregion

  }
}

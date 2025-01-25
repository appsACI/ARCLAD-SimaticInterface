using System;
using System.Collections.Generic;
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
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MaterialLot;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.RoadMap;

namespace SimaticArcorWebApi.Management
{
  public class SimaticRoadMapService : ISimaticRoadMapService
  {
    #region Fields
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<SimaticRoadMapService> logger;

    #endregion

    public ISimaticService SimaticService { get; set; }

    public IUOMService UomService { get; set; }

    public DelegatingHandler HttpClientHandler { get; set; }

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SimaticService"/> class.
    /// </summary>
    public SimaticRoadMapService(ISimaticService simatic, IUOMService uomService)
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticRoadMapService>();

      SimaticService = simatic;

      UomService = uomService;
    }
        #endregion

        #region RoadMap

        public async Task<dynamic> GetRoadMapByNIdAsync(string nid, CancellationToken token)
        {
            using (var client = HttpClientHandler == null ? new AuditableHttpClient(logger) : new AuditableHttpClient(logger, HttpClientHandler))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                HttpResponseMessage response = await client.GetAsync($"sit-svc/application/PICore/odata/ArcorPICore_RoadMap?$filter=NId eq '{nid}'", token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                        return result.value.ToObject<RoadMap[]>();
                    }, token);
            }
        }

    public async Task<dynamic> GetAllRoadMapOperationsAsync(CancellationToken token)
    {
      using (var client = HttpClientHandler == null ? new AuditableHttpClient(logger) : new AuditableHttpClient(logger, HttpClientHandler))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        HttpResponseMessage response = await client.GetAsync($"sit-svc/application/PICore/odata/ArcorPICore_RoadMapOperation?$expand=RoadMap", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
              return result.value.ToObject<RoadMapOperation[]>();
            }, token);
      }
    }

    public async Task<dynamic> GetAllRoadMapOperationsByIdAsync(string id, CancellationToken token)
    {
      using (var client = HttpClientHandler == null ? new AuditableHttpClient(logger) : new AuditableHttpClient(logger, HttpClientHandler))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        HttpResponseMessage response = await client.GetAsync($"sit-svc/application/PICore/odata/ArcorPICore_RoadMap?$filter=Id eq {id}&$expand=RoadMap", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

              if (result.value.Count >= 1)
                return ((IList<RoadMapOperation>)result.value.ToObject<RoadMapOperation[]>()).First();

              dynamic errorMessage = new { Error = "RoadMap not found." };
              throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            }, token);
      }
    }

    public async Task<string> CreateRoadMapAsync(string id, string plant, string type, decimal quantityValue,
        string uoMId, RoadMapRequestOperations[] operations, CancellationToken token)
    {
      logger.LogInformation($"Create roadmap. Item [{id}] Plant [{plant}]");
      using (var client = HttpClientHandler == null ? new AuditableHttpClient(logger) : new AuditableHttpClient(logger, HttpClientHandler))
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
            RoadMapItem = new
            {
              Id = id,
              Plant = plant,
              Type = type,
              QuantityValue = quantityValue,
              uoMNId = UomService.GetSimaticUOM(uoMId),
              Operation = operations.Select(t => new
              {
                t.Id,
                t.Level,
                Name = t.Operation,
                t.KgHs,
                t.KgHsMach,
                t.Theoretical,
                t.Efficiency,
                Crew = t.Crew.GetValueOrDefault().ToString(),
                t.Costs,
                t.MachineHs,
                t.ManHs,
                OpFrom = t.From,
                OpTo = t.To,
                Gas = t.Gas,
                Energy = t.Energia,
                Steam = t.Vapor,
                Gpm = t.GPM,
                Upg = t.UPG,
                Ppu = t.PPU,
                Cdb = t.CDB,
                Gpmd = t.GPMD,
                ErpId = t.ERPID
              })
            }
          }
        });

        var response = await client.PostAsync("sit-svc/application/PICore/odata/ArcorPICore_CreateRoadMap",
            new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
              return result.Id.ToString();
            }, token);
      }
    }

    #endregion
    
  }
}

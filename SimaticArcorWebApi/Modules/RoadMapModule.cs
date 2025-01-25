using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Modules;
using Microsoft.Extensions.Options;
using Common.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Nancy.Json;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Validators.RoadMap;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nancy.Extensions;
using Nancy.IO;
using SimaticArcorWebApi.Model.Config;

namespace SimaticArcorWebApi.Modules
{
  public class RoadMapModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<RoadMapModule> logger;

    private NancyConfig config;

    /// <summary>
    /// Gets or sets the material service component
    /// </summary>
    public IMaterialService MaterialService { get; set; }

    /// <summary>
    /// Gets or sets the simatic service component
    /// </summary>
    public ISimaticRoadMapService SimaticService { get; set; }

    public RoadMapModule(IConfiguration configuration, IMaterialService materialService, ISimaticRoadMapService simaticService) : base("api/roadmaps")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<RoadMapModule>();

      this.MaterialService = materialService;
      this.SimaticService = simaticService;

      config = new NancyConfig();
      configuration.GetSection("NancyConfig").Bind(config);

      Get("/", GetRoadMap, name: "GetRoadMap");

      Get("/{Id}", GetRoadMapById, name: "GetRoadMapById");
      
      Post("/", PostRoadMap, name: "PostRoadMap");

      Post("/bulkInsert", PostRoadMapBulkInsert, name: "PostRoadMapBulkInsert");

    }

    #region Gets

    private async Task<dynamic> GetRoadMapById(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllRoadMapOperationsByIdAsync(((dynamic)parameters).Id.Value, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get a road map");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetRoadMap(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllRoadMapOperationsAsync(ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get a road map");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

    #region Post

    private async Task<dynamic> PostRoadMapBulkInsert(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var rmList = this.BindAndValidate<RoadMapRequest[]>();
        var errorList = new List<KeyValuePair<string, string>>();
        foreach (var rm in rmList)
        {
          try
          {
            var validator = new RoadMapRequestValidator();
            var valResult = validator.Validate(rm);
            if (!valResult.IsValid)
            {
              throw new Exception(string.Join('\n', valResult.Errors));
            }
            //await SimaticService.CreateRoadMapAsync(rm.Description, rm.Efficency, rm.Level, rm.Operation, rm.Theoretical, ct);
          }
          catch (Exception e)
          {
            logger.LogError(e, $"Error trying to create roadMap. Id: [{rm.Id}] Type: [{rm.Type}] Message: [{e.Message}]");
            errorList.Add(new KeyValuePair<string, string>(rm.Id, e.Message));
          }
        }

        if (errorList.Count > 0)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
             errorList.Select(i => new { Id = i.Key, Message = i.Value }));

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create a road map");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> PostRoadMap(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var rm = this.BindAndValidate<RoadMapRequest>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);


        await SimaticService.CreateRoadMapAsync(rm.Id, rm.Plant, rm.Type, rm.QuantityValue, rm.UoMNId, rm.Operations, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create a road map");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create a road map");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

  }

}

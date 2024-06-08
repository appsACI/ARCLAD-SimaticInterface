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
using Microsoft.Extensions.Logging;
using Nancy.Json;
using Nancy.ModelBinding;
using Nancy.Extensions;
using Newtonsoft.Json;
using Serilog;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Validators.Material;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nancy.IO;
using SimaticArcorWebApi.Model.Config;
using Endor.Core.Logger;

namespace SimaticArcorWebApi.Modules
{
  public class MaterialModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<MaterialModule> logger;

    private NancyConfig config;

    /// <summary>
    /// Gets or sets the material service component
    /// </summary>
    public IMaterialService MaterialService { get; set; }

    /// <summary>
    /// Gets or sets the simatic service component
    /// </summary>
    public ISimaticMaterialService SimaticService { get; set; }
    
    public MaterialModule(IConfiguration configuration, IMaterialService materialService, ISimaticMaterialService simaticService) : base("api/materials")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<MaterialModule>();

      this.MaterialService = materialService;
      this.SimaticService = simaticService;
      
      config = new NancyConfig();
      configuration.GetSection("NancyConfig").Bind(config);

      Get("/", GetMaterial, name: "GetMaterial");

      Get("/{Id}", GetMaterialById, name: "GetMaterialById");

      Post("/", PostMaterial, name: "PostMaterial");

      Post("/bulkInsert", PostMaterialBulkInsert, name: "PostMaterialBulkInsert");

      Get("/class", GetMaterialGroup, name: "GetClass");

      Get("/class/{Id}", GetMaterialGroupById, name: "GetMaterialGroupById");

      Get("/definitions", GetMaterialTemplate, name: "GetDefinition");

      Get("/definitions/{Id}", GetMaterialTemplateById, name: "GetMaterialTemplateById");

    }

    #region Gets

    private async Task<dynamic> GetMaterialById(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetMaterialByNIdAsync(((dynamic)parameters).Id.Value, true, true, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e,"Error trying to get materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetMaterial(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllMaterialAsync(ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetMaterialGroup(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllMaterialGroupAsync(ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get material groups");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get material groups");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetMaterialGroupById(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllMaterialGroupByIdAsync(((dynamic)parameters).Id.Value, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get material groups");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get material groups");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetMaterialTemplate(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllMaterialTemplateAsync(ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get material template");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get material template");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> GetMaterialTemplateById(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllMaterialTemplateByIdAsync(((dynamic)parameters).Id.Value, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to get material template");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to get material template");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

    #region Post

    private async Task<dynamic> PostMaterialBulkInsert(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var materialList = this.Bind<MaterialRequest[]>();
        var errorList = new List<KeyValuePair<string, string>>();

        foreach (var mat in materialList)
        {
          try
          {
            var validator = new MaterialValidator();
            var valResult = validator.Validate(mat);
            if (!valResult.IsValid)
            {
              throw new Exception(string.Join('\n', valResult.Errors));
            }

            await MaterialService.ProcessNewMaterialAsync(mat, ct);
          }
          catch (Exception e)
          {
            logger.LogError(e, $"Error trying to create material. Id: [{mat.Id}] Name: [{mat.Name}] Message: [{e.Message}]");
            errorList.Add(new KeyValuePair<string, string>(mat.Id, e.Message));
          }
        }

        if (errorList.Count > 0)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
              errorList.Select(i => new { Id = i.Key, Message = i.Value }));

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> PostMaterial(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var mat = this.BindAndValidate<MaterialRequest>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await MaterialService.ProcessNewMaterialAsync(mat, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create material");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create material");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

  }

}

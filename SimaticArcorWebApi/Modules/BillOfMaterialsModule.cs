using Nancy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Modules;
using Microsoft.Extensions.Options;
using Common.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Nancy.ModelBinding;
using Serilog;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Validators.Bom;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nancy.Extensions;
using Nancy.IO;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom.Bom;

namespace SimaticArcorWebApi.Modules
{
  public class BillOfMaterialsModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<BillOfMaterialsModule> logger;

    private NancyConfig config;

    /// <summary>
    /// Gets or sets the bom service component
    /// </summary>
    public IBOMService BOMService { get; set; }

    /// <summary>
    /// Gets or sets the simatic service component
    /// </summary>
    public ISimaticBOMService SimaticService { get; set; }

    public BillOfMaterialsModule(IConfiguration configuration, IBOMService bomService, ISimaticBOMService simaticService) : base("api/BillOfMaterials")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<BillOfMaterialsModule>();

      this.BOMService = bomService;
      this.SimaticService = simaticService;

      config = new NancyConfig();
      configuration.GetSection("NancyConfig").Bind(config);

      Get("/", GetBillOfMaterials, name: "GetBillOfMaterials");

      Post("/", PostBillOfMaterials, name: "PostBillOfMaterials");

      Post("/bulkInsert", PostBillOfMaterialsBulkInsert, name: "PostBillOfMaterialsBulkInsert");

      //Post("/Conversion", PostBOMConversion, name: "PostBOMConversion");

      //Post("/Conversion/bulkInsert", PostBOMConversionBulkInsert, name: "PostBOMConversionBulkInsert");
    }

    #region Gets

    private async Task<dynamic> GetBillOfMaterials(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var data = await SimaticService.GetAllBillOfMaterialsAsync(ct);

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error getting bill of materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error getting bill of materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

    #region Post
    [Obsolete]
    private async Task<dynamic> PostBOMConversionBulkInsert(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var uomList = this.BindAndValidate<UomConversionRequest[]>();
        var errorList = new List<KeyValuePair<string, string>>();

        foreach (var uom in uomList)
        {
          try
          {
            await BOMService.AddUomConversionEntity(uom, ct);
          }
          catch (Exception e)
          {
            logger.LogError(e, $"Error trying to create UOM conversion. Id: [{uom.Item}] Message: [{e.Message}]");
            errorList.Add(new KeyValuePair<string, string>(uom.Item, e.Message));
          }
        }

        if (errorList.Count > 0)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                  errorList.Select(i => new { Id = i.Key, Message = i.Value }));

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create bill of materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create bill of materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    [Obsolete]
    private async Task<dynamic> PostBOMConversion(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var uom = this.BindAndValidate<UomConversionRequest>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await BOMService.AddUomConversionEntity(uom, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create uom conversion");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create uom conversion");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> PostBillOfMaterialsBulkInsert(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var bomList = this.BindAndValidate<BillOfMaterialsRequest[]>();
        var errorList = new List<KeyValuePair<string, string>>();

        foreach (var bom in bomList)
        {
          try
          {
            var validator = new BillOfMaterialsRequestValidator();
            var valResult = validator.Validate(bom);
            if (!valResult.IsValid)
            {
              throw new Exception(string.Join('\n', valResult.Errors));
            }

            await BOMService.ProcessNewBomAsync(bom, ct);
          }
          catch (Exception e)
          {
            logger.LogError(e, $"Error trying to create BOM. Id: [{bom.Id}] Message: [{e.Message}]");
            errorList.Add(new KeyValuePair<string, string>(bom.Id, e.Message));
          }
        }

        if (errorList.Count > 0)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
              errorList.Select(i => new { Id = i.Key, Message = i.Value }));

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create bill of materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create bill of materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> PostBillOfMaterials(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var bom = this.BindAndValidate<BillOfMaterialsRequest>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await BOMService.ProcessNewBomAsync(bom, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create Bill of materials");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create Bill of materials");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

  }
}

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
using SimaticArcorWebApi.Validators.Equipment;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Nancy.IO;
using SimaticArcorWebApi.Model.Config;
using Endor.Core.Logger;

namespace SimaticArcorWebApi.Modules
{
  public class EquipmentModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<EquipmentModule> logger;

    private NancyConfig config;

    /// <summary>
    /// Gets or sets the material service component
    /// </summary>
    public IEquipmentService EquipmentService { get; set; }

    /// <summary>
    /// Gets or sets the simatic service component
    /// </summary>
    public ISimaticEquipmentService SimaticService { get; set; }
    
    public EquipmentModule(IConfiguration configuration, IEquipmentService equipmentService, ISimaticEquipmentService simaticService) : base("api/equipments")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<EquipmentModule>();

      this.EquipmentService = equipmentService;
      this.SimaticService = simaticService;
      
      config = new NancyConfig();
      configuration.GetSection("NancyConfig").Bind(config);

      //Get("/", GetMaterial, name: "GetMaterial");

      //Get("/{Id}", GetMaterialById, name: "GetMaterialById");

      //Post("/", PostMaterial, name: "PostMaterial");

      Post("/PQRNotification", PostPQRNotificationAsync, name: "PostPQRNotification");
      Post("/PreventiveMaintenance", PreventiveMaintenanceNotificationAsync, name: "EquipmentPreventiveMaintenaceNotification");
    }

    #region Gets

    //private async Task<dynamic> GetMaterialById(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetMaterialByNIdAsync(((dynamic)parameters).Id.Value, true, true, ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get materials");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e,"Error trying to get materials");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    //private async Task<dynamic> GetMaterial(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetAllMaterialAsync(ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get materials");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e, "Error trying to get materials");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    //private async Task<dynamic> GetMaterialGroup(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetAllMaterialGroupAsync(ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get material groups");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e, "Error trying to get material groups");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    //private async Task<dynamic> GetMaterialGroupById(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetAllMaterialGroupByIdAsync(((dynamic)parameters).Id.Value, ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get material groups");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e, "Error trying to get material groups");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    //private async Task<dynamic> GetMaterialTemplate(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetAllMaterialTemplateAsync(ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get material template");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e, "Error trying to get material template");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    //private async Task<dynamic> GetMaterialTemplateById(dynamic parameters, CancellationToken ct)
    //{
    //  try
    //  {
    //    var data = await SimaticService.GetAllMaterialTemplateByIdAsync(((dynamic)parameters).Id.Value, ct);

    //    return Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new { data });
    //  }
    //  catch (SimaticApiException e)
    //  {
    //    logger.LogError(e, "Error trying to get material template");
    //    return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
    //  }
    //  catch (Exception e)
    //  {
    //    logger.LogError(e, "Error trying to get material template");
    //    return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
    //  }
    //}

    #endregion

    #region Post

    private async Task<dynamic> PostPQRNotificationAsync(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var equPqr = this.BindAndValidate<EquipmentPQRNotification>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await EquipmentService.ProcessNewPQRAsync(equPqr, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to deliver PQR For Equipment");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to deliver PQR For Equipment");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    private async Task<dynamic> PreventiveMaintenanceNotificationAsync(dynamic parameters, CancellationToken ct)
    {


      /*
      “OrderId”: ”SO00000030”
      "EquipmentNId": "CORTADORA KAMPF 1 | CORTADORA KAMPF 2",
      "Tipo": "Preventive Maintenance | Reactive Maintenance",
      "DateStart": "19/05/2022 10:00 am",
      "DateEnd": "19/05/2022 11:00 am",
      "Status": "C Scheduled | H Completed | X Cancelled",
      "Comments": "el equipo se paró por falla mecánica."
      */

      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        EquipmentPreventiveMaintenaceNotification equPreventiveMaintenace = this.BindAndValidate<EquipmentPreventiveMaintenaceNotification>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await EquipmentService.ProcessPreventiveMaitenanceDataAsync(equPreventiveMaintenace, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Accepted);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, $"Error trying to set Equipment preventive maintenance data. Exeption: {e}");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, $"Error trying to set Equipment preventive maintenance data. Exeption: {e}. Please, validate data types!");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion

  }

}

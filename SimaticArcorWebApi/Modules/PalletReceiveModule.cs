using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using Serilog;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.PalletReception;
using SimaticArcorWebApi.Validators.Order;

namespace SimaticArcorWebApi.Modules
{
  public class PalletReceiveModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<PalletReceiveModule> logger;

    private NancyConfig config;

    /// <summary>
    /// Gets or sets the material service component
    /// </summary>
    public IOrderService OrderService { get; set; }

    /// <summary>
    /// Gets or sets the simatic service component
    /// </summary>
    public ISimaticMTUService SimaticService { get; set; }

    public PalletReceiveModule(IConfiguration configuration, IOrderService orderService, ISimaticMTUService simaticService) : base("api/pallet")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<PalletReceiveModule>();

      this.OrderService = orderService;
      this.SimaticService = simaticService;

      config = new NancyConfig();
      configuration.GetSection("NancyConfig").Bind(config);

      base.Get("/docs", x =>
      {
        var site = Request.Url.SiteBase;
        return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
      });

      Post("/reception", Create, name: "CreatePalletReception");
    }

    #region Post
    
    private async Task<dynamic> Create(dynamic parameters, CancellationToken ct)
    {
      try
      {
        if (config.EnableRequestLogging)
        {
          logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
          Request.Body.Position = 0;
        }

        var req = this.BindAndValidate<CreatePalletReceptionRequest>();

        if (!this.ModelValidationResult.IsValid)
          return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

        await SimaticService.CreatePalletReception(req, ct);

        return Negotiate.WithStatusCode(HttpStatusCode.Created);
      }
      catch (SimaticApiException e)
      {
        logger.LogError(e, "Error trying to create a pallet reception");
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        logger.LogError(e, "Error trying to create a pallet reception");
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

    #endregion
  }
}

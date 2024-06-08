using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;

namespace SimaticArcorWebApi.Modules
{
    public class BulkReceptionModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<BulkReceptionModule> logger;

        private NancyConfig config;

        /// <summary>
        /// Gets or sets the bulk reception service component
        /// </summary>
        public IBulkReceptionService BulkReceptionService { get; set; }

        public BulkReceptionModule(IConfiguration configuration, IBulkReceptionService bulkReceptionService) : base("api/bulkreception")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<BulkReceptionModule>();

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            this.BulkReceptionService = bulkReceptionService;

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            Post("/TruckReceive", TruckReceive, name: "TruckReceive");

            Post("/Confirm", Confirm, name: "Confirm");

        }

        #region Post
        private async Task<dynamic> TruckReceive(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<TruckReceiveRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await BulkReceptionService.TruckReceiveProcessingAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to receive Truck Request");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to receive truck Request");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> Confirm(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<TruckReceiveRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await BulkReceptionService.ConfirmBulkReceptionAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to confirm truck reception");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to confirm truck reception");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }
        #endregion

    }
}

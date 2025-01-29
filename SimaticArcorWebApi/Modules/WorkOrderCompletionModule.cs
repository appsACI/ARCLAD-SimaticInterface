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
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Validators.WorkOrderCompletion;


namespace SimaticArcorWebApi.Modules
{
    public class WorkOrderCompletionModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<WorkOrderCompletionModule> logger;

        private NancyConfig config;


        public IWorkOrderCompletionService WoCService { get; set; }
        public IWorkOrderCompletionConsumoService WoCCService { get; set; }


        public ISimaticWorkOrderCompletionService SimaticService { get; set; }

        public WorkOrderCompletionModule(IConfiguration configuration, IWorkOrderCompletionService WoCService, IWorkOrderCompletionConsumoService WoCCService, ISimaticWorkOrderCompletionService simaticService) : base("api/WoCompletion")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<WorkOrderCompletionModule>();

            this.WoCService = WoCService;
            this.WoCCService = WoCCService;
            this.SimaticService = simaticService;

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            Post("/", CreateWoCompletionAsync, name: "CreateWoCompletion");
            Post("/woCompletionVinilos", CreateWoCompletionVinilosAsync, name: "CreateWoCompletionVinilos");

            Post("/woCompletionConsumo", CreateWoCompletionConsumoAsync, name: "CreateWoCompletionConsumo");
        }

        #region Post

        public async Task<dynamic> CreateWoCompletionAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:{RequestStream.FromStream(Request.Body).AsString()}");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<WorkOrderCompletionModel>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }



                CancellationTokenSource tokenSource = new CancellationTokenSource();

                string WoResponse = await WoCService.CreateWoCompletionAsync(prod, ct);
                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(WoResponse);

            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to send WoCompletion");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        public async Task<dynamic> CreateWoCompletionVinilosAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:{RequestStream.FromStream(Request.Body).AsString()}");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<WorkOrderCompletionVinilosModel>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }



                CancellationTokenSource tokenSource = new CancellationTokenSource();

                string WoResponse = await WoCService.CreateWoCompletionVinilosAsync(prod, ct);
                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(WoResponse);

            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to send WoCompletion");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        public async Task<dynamic> CreateWoCompletionConsumoAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:{RequestStream.FromStream(Request.Body).AsString()}");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<WorkOrderCompletionConsumoModel>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                string WoResponse= await WoCCService.CreateWoCompletionConsumoAsync(prod,ct);
                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(WoResponse);

            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion
    }
}

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
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticArcorWebApi.Validators.Order;

namespace SimaticArcorWebApi.Modules
{
    public class PQRLMSModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<PQRLMSModule> logger;

        private NancyConfig config;

        /// <summary>
        /// Gets or sets the material service component
        /// </summary>
        public IPQRLMSService PQRLMSService { get; set; }

        /// <summary>
        /// Gets or sets the simatic service component
        /// </summary>
        public ISimaticOrderService SimaticService { get; set; }

        public PQRLMSModule(IConfiguration configuration, IPQRLMSService pqrlmsService, ISimaticOrderService simaticService) : base("api/pqr_lms")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<PQRLMSModule>();

            this.PQRLMSService = pqrlmsService;
            this.SimaticService = simaticService;

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            //Post("/", CreateOrdersAsync, name: "CreateOrders");

            //Get("/{NId}", GetWorkOrderStatus, name: "GetOrderStatus");

            Post("/insert", CreatePQRLMSInsertAsync, name: "CreatePQRLMSInsert");
        }

        #region Get
                

        #endregion

        #region Post

        public async Task<dynamic> CreatePQRLMSInsertAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                PQRLMSResponse response = new PQRLMSResponse();
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                var productionList = this.BindAndValidate<PQRLMSRequest>();
                var errorList = new List<KeyValuePair<string, string>>();

                try
                {
                    var validator = new PQRLMSRequestValidator();
                    var valResult = validator.Validate(productionList);
                    if (!valResult.IsValid)
                    {
                        throw new Exception(string.Join('\n', valResult.Errors));
                    }

                    response = await PQRLMSService.ProcessNewPQRLMSAsync(productionList, ct);
                }
                catch (Exception e)
                {
                    logger.LogError($"Error trying to create order. Order Id: [{productionList.Title}]");
                    errorList.Add(new KeyValuePair<string, string>(productionList.Title, e.Message));
                }

                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                        errorList.Select(i => new { Id = i.Key, Message = i.Value }));

                //return Negotiate.WithStatusCode(HttpStatusCode.Created);
                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(response);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create orders");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create orders");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion
    }
}

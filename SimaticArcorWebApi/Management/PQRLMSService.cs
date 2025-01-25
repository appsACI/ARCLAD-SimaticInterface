using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.BOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using Newtonsoft.Json;

namespace SimaticArcorWebApi.Management
{
    public class PQRLMSService : IPQRLMSService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<OrderService> logger;

        public ISimaticNetsuitService SimaticNetsuitService { get; set; }

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public ISimaticBOMService SimaticBOMService { get; set; }

        public PQRLMSService(ISimaticNetsuitService simaticnetsuit, ISimaticMaterialService simaticMaterialService, ISimaticBOMService simaticBOMService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<OrderService>();

            SimaticNetsuitService = simaticnetsuit;
        }

        public async Task<PQRLMSResponse> ProcessNewPQRLMSAsync(PQRLMSRequest prod, CancellationToken ct)
        {
            string verb = "CreateOrdenOperation";

            logger.LogInformation($"A new Order Operation data is received, analysing Order ID [{prod.Title}] WorkOrder [{prod.Title}] for Final Material [{prod.Title}] ...");

            if (!string.IsNullOrEmpty(prod.Title))
            {
                var respostring = await SimaticNetsuitService.CreatePQRLMSAsync(prod, verb, ct);
                dynamic respo = JsonConvert.DeserializeObject<dynamic>(respostring);
                dynamic response = new PQRLMSResponse
                {
                    message = "OK",
                    response = respo.isSuccess.ToObject<bool>() ? respo.data.ToObject<IncommingMessage>() : null
                };

                logger.LogInformation($"Order completion [{prod.Title}] send successfully, the response is: '{respo}'");
                logger.LogInformation($"RespondePqr '{response}'");

                return response;


            }

            return null;
        }


    }
}

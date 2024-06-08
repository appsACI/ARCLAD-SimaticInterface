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
            dynamic response = new PQRLMSResponse();

            if (!string.IsNullOrEmpty(prod.Title))
            {
                var respo = await SimaticNetsuitService.CreatePQRLMSAsync(prod, verb, ct);

                dynamic jsonResponse = JsonConvert.DeserializeObject(respo);

                string messag = jsonResponse.isSuccess;

                string caseID = jsonResponse.data.supportCaseId;

                response.message = messag;
                //response.response.supportCaseId = caseID;
                response.response = new IncommingMessage();
                response.response.supportCaseId = caseID;
                //Cambiar de String a PQRLMSResponse

            }

            return response;
        }


    }
}

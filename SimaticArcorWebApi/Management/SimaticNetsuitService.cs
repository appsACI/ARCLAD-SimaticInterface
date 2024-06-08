using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Logging;
using Nancy.Json;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using SimaticArcorWebApi.Model.Simatic;
using SimaticWebApi.Modules.oauth1;


namespace SimaticArcorWebApi.Management
{
    public class SimaticNetsuitService : ISimaticNetsuitService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticNetsuitService> logger;

        #endregion

        public ISimaticService SimaticService { get; set; }

        public IUOMService UomService { get; set; }

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticNetsuitService"/> class.
        /// </summary>
        public SimaticNetsuitService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticNetsuitService>();

            SimaticService = simatic;
            UomService = uomService;
        }
        #endregion

        #region Interface Implementation

        public async Task<dynamic> CreatePQRLMSAsync(PQRLMSRequest PQRLMSCasesData, string module, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_ar_create_case&deploy=customdeploy_ar_create_case";

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var oauthParams = new NSOAuth1(
                     method: "POST",
                     url: URL,
                     ck: "3ead249d57802995a6c1829d0a8977a55931eff8effe0093a8c4a149c5fae5fb",
                     cs: "9501f2383381fb869e0794a7d3d71e67e4a1d46cf73c93edbbcad4fed13c99c0",
                     tk: "ab51dceb423ded3cc8a797ae483fca69addbcfc7c256491434113ac62452e962",
                     ts: "e6ac1de9b39103c4ccc0b92f3fe9405f0a1b8b186d77fc0c3a80a5f6e24a266f",
                     realm: "5842241_SB1"
                );

                // Generar la firma OAuth
                var oauthInfo = oauthParams.GenerateOAuth();


                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", oauthInfo.ToString());

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new PQRLMSRequest
                {
                    Title = PQRLMSCasesData.Title,
                    EquipmentNId = PQRLMSCasesData.EquipmentNId,
                    IncommingMessage = PQRLMSCasesData.IncommingMessage,
                    Tipo = PQRLMSCasesData.Tipo
                });


                logger.LogInformation("payload: " + json);

                var response = await client.PostAsync(URL, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);



                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToString();
                  });
            }
        }

        #endregion

    }
}

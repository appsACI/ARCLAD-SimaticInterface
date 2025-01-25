using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core;
using Endor.Core.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;
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

        #region -- NS varibles -- 
        private readonly string nsURL = "/app/site/hosting/restlet.nl?script=customscript_ar_create_case&deploy=customdeploy_ar_create_case";
        private readonly string ck = "";
        private readonly string cs = "";
        private readonly string tk = "";
        private readonly string ts = "";
        private readonly string realm = "";
        private readonly string UrlBase = "";
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticNetsuitService"/> class.
        /// </summary>
        public SimaticNetsuitService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticNetsuitService>();

            SimaticService = simatic;
            UomService = uomService;

            var config = ServiceInstance.Configuration;
            var NetSuiteConfig = new NetSuiteConfigDto();
            config.GetSection("NetSuite").Bind(NetSuiteConfig);

            this.UrlBase = NetSuiteConfig.BaseUrl;
            this.ck = NetSuiteConfig.ck;
            this.cs = NetSuiteConfig.cs;
            this.tk = NetSuiteConfig.tk;
            this.ts = NetSuiteConfig.ts;
            this.realm = NetSuiteConfig.realm;

        }
        #endregion

        #region Interface Implementation

        public async Task<dynamic> CreatePQRLMSAsync(PQRLMSRequest PQRLMSCasesData, string module, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                //var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_ar_create_case&deploy=customdeploy_ar_create_case";

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var oauthParams = new NSOAuth1(
                        method: "POST",
                        url: this.UrlBase + this.nsURL,
                        ck: this.ck,
                        cs: this.cs,
                        tk: this.tk,
                        ts: this.ts,
                        realm: this.realm
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


                logger.LogInformation("payload pqr NS: " + json);

                var response = await client.PostAsync(this.UrlBase + this.nsURL, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);
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

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
using SimaticWebApi.Model.Custom.ProductionTime;
using SimaticWebApi.Modules.oauth1;


namespace SimaticArcorWebApi.Management
{
    public class SimaticWorkOrderCompletionService : ISimaticWorkOrderCompletionService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticWorkOrderCompletionService> logger;


        #endregion

        public ISimaticService SimaticService { get; set; }

        public IUOMService UomService { get; set; }

        #region -- NS varibles --
        private readonly string nsURL = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";
        private readonly string nsURL2 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_issue&deploy=customdeploy_bit_rl_work_order_issue";
        private readonly string nsURL3 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_woc_vinyls&deploy=customdeploy_bit_rl_woc_vinyls";
        private readonly string ck = "";
        private readonly string cs = "";
        private readonly string tk = "";
        private readonly string ts = "";
        private readonly string realm = "";
        private readonly string UrlBase = "";

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticWorkOrderCompletionService"/> class.
        /// </summary>
        public SimaticWorkOrderCompletionService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticWorkOrderCompletionService>();

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

        public async Task<dynamic> GetProductionTimeAsync(string WorkOrderId, CancellationToken token)
        {

            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/FBLMS/odata/vTiemposProduccion?$filter=Orden eq '{WorkOrderId}' and Estado eq 'Total'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                        {
                            logger.LogInformation($"Tiempos de produccion {result}");
                            return (IList<ProductionTime>)result.value.ToObject<ProductionTime[]>();

                        }
                        else
                        {
                            logger.LogInformation($"Tiempos de produccion {result}");
                            return (IList<ProductionTime>)result.value.ToObject<ProductionTime[]>();
                        }

                        return new List<ProductionTime>();

                    }, token);
            }
        }

        public async Task<dynamic> CreateWoCompletionAsync(WorkOrderCompletionModel woCompletion, CancellationToken ct)
        {
            if (!woCompletion.isbackflush)
            {
                using (var client = new AuditableHttpClient(logger))
                {
                    client.BaseAddress = new Uri(SimaticService.GetUrl());

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

                    var jsonBackFlush = JsonConvert.SerializeObject(new
                    {
                        woChildrenId = woCompletion.woChildrenId,
                        location = woCompletion.location,
                        publishedDate = woCompletion.publishedDate,
                        startTime = woCompletion.startTime,
                        endTime = woCompletion.endTime,
                        production = woCompletion.production,
                        quantity = woCompletion.quantity,
                        startOperation = woCompletion.startOperation,
                        endOperation = woCompletion.endOperation,
                        memo = woCompletion.memo,
                        employee = woCompletion.employee,
                        completed = woCompletion.completed,
                        isbackflush = woCompletion.isbackflush,
                        flagTrim = woCompletion.flagTrim,
                        employeeComplement = woCompletion.employeeComplement,
                        detail = woCompletion.detail,
                        scrap = woCompletion.scrap

                    });

                    logger.LogInformation($"payload: [{jsonBackFlush }] ");


                    return await response.Content.ReadAsStringAsync()
                      .ContinueWith(task =>
                      {
                          var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                          return result.ToString();
                      });
                }
            }

            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";

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
                var json = JsonConvert.SerializeObject(new WorkOrderCompletionModel
                {
                    woChildrenId = woCompletion.woChildrenId,
                    location = woCompletion.location,
                    publishedDate = woCompletion.publishedDate,
                    startTime = woCompletion.startTime,
                    endTime = woCompletion.endTime,
                    production = woCompletion.production,
                    quantity = woCompletion.quantity,
                    startOperation = woCompletion.startOperation,
                    endOperation = woCompletion.endOperation,
                    memo = woCompletion.memo,
                    employee = woCompletion.employee,
                    completed = woCompletion.completed,
                    isbackflush = woCompletion.isbackflush,
                    flagTrim = woCompletion.flagTrim,
                    employeeComplement = woCompletion.employeeComplement,
                    detail = woCompletion.detail,
                    materialConsumedActual = woCompletion.materialConsumedActual,
                    scrap = woCompletion.scrap

                });

                logger.LogInformation($"payload: {json}");

                var response = await client.PostAsync(this.UrlBase + this.nsURL, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToString();
                  });
            }
        }

        public async Task<dynamic> CreateWoCompletionVinilosAsync(WorkOrderCompletionVinilosModel woCompletion, CancellationToken ct)
        {
            if (!woCompletion.isbackflush)
            {
                using (var client = new AuditableHttpClient(logger))
                {
                    client.BaseAddress = new Uri(SimaticService.GetUrl());

                    // We want the response to be JSON.
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var oauthParams = new NSOAuth1(
                         method: "POST",
                         url: this.UrlBase + this.nsURL3,
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
                    
                    var jsonBackFlush = JsonConvert.SerializeObject(new
                    {
                        woChildrenId = woCompletion.woChildrenId,
                        location = woCompletion.location,
                        publishedDate = woCompletion.publishedDate,
                        startTime = woCompletion.startTime,
                        endTime = woCompletion.endTime,
                        production = woCompletion.production,
                        quantity = woCompletion.quantity,
                        startOperation = woCompletion.startOperation,
                        endOperation = woCompletion.endOperation,
                        memo = woCompletion.memo,
                        employee = woCompletion.employee,
                        completed = woCompletion.completed,
                        isbackflush = woCompletion.isbackflush,
                        flagTrim = woCompletion.flagTrim,
                        employeeComplement = woCompletion.employeeComplement,
                        detail = woCompletion.detail,
                        scrap = woCompletion.scrap

                    });

                    logger.LogInformation($"payload: [{jsonBackFlush }] ");

                    var response = await client.PostAsync(this.UrlBase + this.nsURL3, new StringContent(jsonBackFlush, Encoding.UTF8, "application/json")).ConfigureAwait(true);

                    return await response.Content.ReadAsStringAsync()
                      .ContinueWith(task =>
                      {
                          var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                          return result.ToString();
                      });
                }
            }

            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());


                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var oauthParams = new NSOAuth1(
                           method: "POST",
                           url: this.UrlBase + this.nsURL3,
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
                var json = JsonConvert.SerializeObject(new WorkOrderCompletionVinilosModel
                {
                    woChildrenId = woCompletion.woChildrenId,
                    location = woCompletion.location,
                    publishedDate = woCompletion.publishedDate,
                    startTime = woCompletion.startTime,
                    endTime = woCompletion.endTime,
                    production = woCompletion.production,
                    quantity = woCompletion.quantity,
                    startOperation = woCompletion.startOperation,
                    endOperation = woCompletion.endOperation,
                    memo = woCompletion.memo,
                    employee = woCompletion.employee,
                    completed = woCompletion.completed,
                    isbackflush = woCompletion.isbackflush,
                    flagTrim = woCompletion.flagTrim,
                    employeeComplement = woCompletion.employeeComplement,
                    detail = woCompletion.detail,
                    materialConsumedActual = woCompletion.materialConsumedActual,
                    scrap = woCompletion.scrap

                });

                logger.LogInformation($"payload: {json}");

                var response = await client.PostAsync(this.UrlBase + this.nsURL3, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToString();
                  });
            }
        }

        public async Task<dynamic> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel woCompletionConsumo)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var oauthParams = new NSOAuth1(
                          method: "POST",
                          url: this.UrlBase + this.nsURL2,
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
                var json = JsonConvert.SerializeObject(new WorkOrderCompletionConsumoModel
                {
                    woChildrenId = woCompletionConsumo.woChildrenId,
                    location = woCompletionConsumo.location,
                    publishedDate = woCompletionConsumo.publishedDate,
                    startOperation = woCompletionConsumo.startOperation,
                    endOperation = woCompletionConsumo.endOperation,
                    memo = woCompletionConsumo.memo,
                    flagTrim = woCompletionConsumo.flagTrim,
                    materialConsumedActual = woCompletionConsumo.materialConsumedActual,

                });

                logger.LogInformation("payload: " + json);

                var response = await client.PostAsync(this.UrlBase + this.nsURL2, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);

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

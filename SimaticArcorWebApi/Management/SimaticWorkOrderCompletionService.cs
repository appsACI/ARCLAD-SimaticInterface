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

        public ISimaticOrderService OrderService { get; set; }


        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticWorkOrderCompletionService"/> class.
        /// </summary>
        public SimaticWorkOrderCompletionService(ISimaticService simatic, IUOMService uomService, ISimaticOrderService orderService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticWorkOrderCompletionService>();

            SimaticService = simatic;
            UomService = uomService;
            OrderService = orderService;
        }
        #endregion

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

                var url = $"sit-svc/Application/FBLMS/odata/vTiemposProduccion?$filter=Orden eq '{WorkOrderId}' and Estado eq 'Productivo'";

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
                            //return "";
                        }
                        dynamic errorMessage = new { Error = "Work Order not found." };
                        throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                    }, token);
            }
        }

        #region Interface Implementation

        public async Task<string> CreateWoCompletionAsync(WorkOrderCompletionModel woCompletion, CancellationToken ct)
        {
            var infoWO = await OrderService.GetOrderByNameAsync(woCompletion.woChildrenId.ToString(), ct);

            var p = await GetProductionTimeAsync(infoWO.NId, ct);
            if (p.length > 0)
            {
                logger.LogInformation($"Adding operation time to payload");
                woCompletion.startTime = p[0].StartTime;
                woCompletion.endTime = p[0].EndTime;
            }
            logger.LogInformation($"Datos a enviar a netsuit {woCompletion}");
            if (!woCompletion.isbackflush)
            {
                using (var client = new AuditableHttpClient(logger))
                {
                    client.BaseAddress = new Uri(SimaticService.GetUrl());

                    var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";

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


                    var response = await client.PostAsync(URL, new StringContent(jsonBackFlush, Encoding.UTF8, "application/json")).ConfigureAwait(true);




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

                string name = Convert.ToString(woCompletion.woChildrenId);

                var infoWO = await OrderService.GetOrderByNameAsync(name, ct);

                var p = await GetProductionTimeAsync(infoWO.NId, ct);
                if (p.length > 0)
                {
                    logger.LogInformation($"Adding operation time to payload");
                    woCompletion.startTime = p[0].StartTime;
                    woCompletion.endTime = p[0].EndTime;
                }
                logger.LogInformation($"Datos a enviar a netsuit {woCompletion}");
                var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";

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

                logger.LogInformation("payload: ", json);

                var response = await client.PostAsync(URL, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(true);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToString();
                  });
            }
        }


        public async Task<string> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel woCompletionConsumo)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                var URL = "https://5842241-sb1.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_issue&deploy=customdeploy_bit_rl_work_order_issue";

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

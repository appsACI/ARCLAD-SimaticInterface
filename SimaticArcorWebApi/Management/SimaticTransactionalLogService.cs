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
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using SimaticArcorWebApi.Model.Simatic;
using SimaticWebApi.Model.Custom.ProductionTime;
using SimaticWebApi.Model.Custom.TransactionalLog;
using SimaticWebApi.Modules.oauth1;

namespace SimaticWebApi.Management
{

    public class SimaticTransactionalLogService : ISimaticTransactionalLogService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticTransactionalLogService> logger;

        #endregion

        public ISimaticService SimaticService { get; set; }


        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticTransactionalLogService"/> class.
        /// </summary>
        public SimaticTransactionalLogService(ISimaticService simatic)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticTransactionalLogService>();

            SimaticService = simatic;


            var config = ServiceInstance.Configuration;

        }
        #endregion

        public async Task<dynamic> CreateTLog(TransactionalLogModel log, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new TransactionalLogModel
                    {
                        Planta = log.Planta,
                        Proceso = log.Proceso,
                        Maquina = log.Maquina,
                        Tipo = log.Tipo,
                        WorkOrders = log.WorkOrders,
                        Lotes = log.Lotes,
                        Descripcion = log.Descripcion,
                        Trim = log.Trim,
                        NTrim = log.NTrim,
                        Cortes = log.Cortes,
                        URL = log.URL,
                        Payload = log.Payload,
                        ErrorMessage = log.ErrorMessage,
                        Succeced = log.Succeced,
                        Comando = log.Comando,
                        ProgramaDestino = log.ProgramaDestino,
                        ProgramaFuente = log.ProgramaFuente,
                        ReasonStatus = log.ReasonStatus
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/TransactionLogs/odata/CreateLogRecord",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToString();
                  }, ct);
            }
        }

    }
}

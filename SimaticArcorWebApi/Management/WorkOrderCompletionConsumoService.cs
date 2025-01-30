using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;

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
using SimaticWebApi.Model.Custom.TransactionalLog;
using Newtonsoft.Json.Linq;
using SimaticWebApi.Management;
using Endor.Core;
using SimaticArcorWebApi.Model.Custom;
using Microsoft.Extensions.Configuration;

namespace SimaticArcorWebApi.Management
{
    public class WorkOrderCompletionConsumoService : IWorkOrderCompletionConsumoService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<WorkOrderCompletionConsumoService> logger;

        public ISimaticService SimaticService { get; set; }
        public ISimaticWorkOrderCompletionService SimaticWorkOrderCompletionService { get; set; }
        public ISimaticOrderService OrderService { get; set; }
        public ISimaticTransactionalLogService TransactionalLogService { get; set; }

        private readonly string nsURL = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";
        private readonly string nsURL2 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_issue&deploy=customdeploy_bit_rl_work_order_issue";
        private readonly string nsURL3 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_woc_vinyls&deploy=customdeploy_bit_rl_woc_vinyls";
        private readonly string UrlBase = "";

        public WorkOrderCompletionConsumoService(ISimaticService simatic, ISimaticWorkOrderCompletionService SimaticWOCompletionService, ISimaticOrderService orderService, ISimaticTransactionalLogService transactionalLogService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<WorkOrderCompletionConsumoService>();

            SimaticService = simatic;
            SimaticWorkOrderCompletionService = SimaticWOCompletionService;
            OrderService = orderService;
            TransactionalLogService = transactionalLogService;

            var config = ServiceInstance.Configuration;
            var NetSuiteConfig = new NetSuiteConfigDto();
            config.GetSection("NetSuite").Bind(NetSuiteConfig);

            this.UrlBase = NetSuiteConfig.BaseUrl;
        }

        public async Task<dynamic> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel prod, CancellationToken ct)
        {

            string woId = string.Empty;
            string workwoId = string.Empty;
            string verb = string.Empty;

            logger.LogInformation($"A new Order data is received, analysing Order ID [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] for Final Material [{prod.materialConsumedActual[0].materialDefinitionId}] ...");


            #region -- Create / ReCreate Order & WorkOrder --

            string name = Convert.ToString(prod.woChildrenId);

            Order infoOrder = await OrderService.GetOrderByNameAsync(name, ct);
            WorkOrderOperation infoWOrderOp = await OrderService.GetWorkOrderOperationBySequenceAsync(prod.startOperation.ToString(), ct);
            // This operation always recreates an Order, this is IOInteroperability standard behaviour.
            woId = await SimaticWorkOrderCompletionService.CreateWoCompletionConsumoAsync(prod);
            logger.LogInformation($"Order completion only consum [{prod.woChildrenId}] - [{prod.woChildrenId}] created/recreated successfully with ID '{woId}'");


            #region TRANSACTIONAL LOG


            JArray resTemp;

            try
            {
                JToken token = JToken.Parse((string)woId);
                resTemp = token is JArray array ? array : new JArray(token);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing woId: {ex.Message}");
                resTemp = new JArray(); // En caso de error, asigna un array vacío
            }

            logger.LogInformation($"JSON : '{resTemp}'");

            JObject res = new JObject();

            if (resTemp.Count > 0)
            {
                res = (JObject)resTemp[0];
            }

            logger.LogInformation($"JSON : '{res}'");

            var newlog = new TransactionalLogModel
            {
                Planta = prod.location,
                Proceso = infoWOrderOp.Name,
                Maquina = prod.materialConsumedActual[0]?.inventory[0]?.binnumber,
                Tipo = "COMPLETION",
                WorkOrders = infoOrder.NId,
                Lotes = "",
                Descripcion = "",
                Trim = "",
                NTrim = "",
                Cortes = "",
                Payload = prod.ToString(),
            };


            newlog.ErrorMessage = res.ToString();
            newlog.ReasonStatus = res["error"]?["message"]?.ToString() != null ? res["error"]?["message"]?.ToString() : "Message Vacio";
            newlog.Succeced = res["isSuccess"]?.ToString() != null ? res["isSuccess"]?.ToString() : "False";
            newlog.Comando = "CreateWoCompletionAsync";
            newlog.ProgramaFuente = "NETSUITE";
            newlog.ProgramaDestino = "OPCENTER";
            newlog.URL = this.UrlBase + this.nsURL;
            await TransactionalLogService.CreateTLog(newlog, ct);

            #endregion

            logger.LogInformation($"Order completion Consumo [{prod.woChildrenId}] send successfully with ID '{woId}'");

            return woId;



            #endregion
        }



    }
}

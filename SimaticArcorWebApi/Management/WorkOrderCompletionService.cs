using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;

using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;
using Nancy;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticWebApi.Model.Custom.Counters;
using SimaticWebApi.Model.Custom.ProductionTime;
using SimaticWebApi.Model.Custom.TransactionalLog;
using Newtonsoft.Json;
using SimaticWebApi.Management;
using Newtonsoft.Json.Linq;
using Endor.Core;
using SimaticArcorWebApi.Model.Custom;
using Microsoft.Extensions.Configuration;

namespace SimaticArcorWebApi.Management
{
    public class WorkOrderCompletionService : IWorkOrderCompletionService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<WorkOrderCompletionService> logger;

        public ISimaticService SimaticService { get; set; }
        public ISimaticWorkOrderCompletionService SimaticWorkOrderCompletionService { get; set; }
        public ISimaticOrderService OrderService { get; set; }
        public ISimaticTransactionalLogService TransactionalLogService { get; set; }

        private readonly string nsURL = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_completio&deploy=customdeploy_bit_rl_work_order_completio";
        private readonly string nsURL2 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_work_order_issue&deploy=customdeploy_bit_rl_work_order_issue";
        private readonly string nsURL3 = "/app/site/hosting/restlet.nl?script=customscript_bit_rl_woc_vinyls&deploy=customdeploy_bit_rl_woc_vinyls";
        private readonly string UrlBase = "";

        #region Constructor

        public WorkOrderCompletionService(ISimaticService simatic, ISimaticWorkOrderCompletionService SimaticWOCompletionService, ISimaticOrderService orderService, ISimaticTransactionalLogService transactionalLogService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<WorkOrderCompletionService>();

            SimaticService = simatic;
            SimaticWorkOrderCompletionService = SimaticWOCompletionService;
            OrderService = orderService;
            TransactionalLogService = transactionalLogService;

            var config = ServiceInstance.Configuration;
            var NetSuiteConfig = new NetSuiteConfigDto();
            config.GetSection("NetSuite").Bind(NetSuiteConfig);

            this.UrlBase = NetSuiteConfig.BaseUrl;
        }
        #endregion

        public async Task<dynamic> CreateWoCompletionAsync(WorkOrderCompletionModel prod, CancellationToken ct)
        {

            logger.LogInformation($"A new WOCompletion data is received, analysing woChildrenId [{prod.woChildrenId}] ...");

            #region -- Create WoCompletion --
            logger.LogInformation($"preparacion de Datos a enviar a netsuit {prod}");
            string name = Convert.ToString(prod.woChildrenId);

            #region GET DATA
            Order infoOrder = await OrderService.GetOrderByNameAsync(name, ct);
            WorkOrder infoWOrder = await OrderService.GetWorkOrderByNIdAsync(infoOrder.NId, false, ct);
            WorkOrderOperation infoWOrderOp = await OrderService.GetWorkOrderOperationBySequenceAsync(prod.startOperation.ToString(), ct);
            WorkOrderExtended infoWOrderExtend = await OrderService.GetWorkOrderExtendedByOrderNIdAsync(infoOrder.NId, false, ct);
            var ultimaDeclaracion = await OrderService.GetWorkOrderParameterUltimaDeclarion(infoWOrder.Id, ct);
            ProductionTime[] timeProd = await SimaticWorkOrderCompletionService.GetProductionTimeAsync(infoOrder.NId, ct);
            #endregion

            DateTime ultDec = ultimaDeclaracion.Length <= 0 ? new DateTime() : Convert.ToDateTime(ultimaDeclaracion[0].ParameterTargetValue);
            DateTimeOffset fechaOffset = new DateTimeOffset(ultDec);

            // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
            string fechaFormatoISO = fechaOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            logger.LogInformation($"Modificando tiempos del WoCompletion");

            if (timeProd.Length > 0)
            {
                logger.LogInformation($"prod Start: {timeProd[0].StartTime}");

                DateTime prodStart = Convert.ToDateTime(timeProd[0].StartTime);
                DateTimeOffset fechaOffsetProdstar = new DateTimeOffset(prodStart);
                string fechaFormatoISOProdstart = fechaOffsetProdstar.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                logger.LogInformation($"prod end: {timeProd[0].EndTime}");
                DateTime prodEnd = Convert.ToDateTime(timeProd[0].EndTime);
                DateTimeOffset fechaOffsetProdEnd = new DateTimeOffset(prodEnd);
                string fechaFormatoISOProdEnd = fechaOffsetProdEnd.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                logger.LogInformation($"prod end iso: {fechaFormatoISOProdEnd}");

                logger.LogInformation($"Adding operation time lms to payload");
                prod.startTime = ultimaDeclaracion.Length <= 0 ? fechaFormatoISOProdstart : fechaFormatoISO;
                prod.endTime = fechaFormatoISOProdEnd;
                logger.LogInformation($"tiempos modificandos lms{prod}");

            }
            else
            {
                logger.LogInformation($"Adding operation time actualStartTime to payload");
                prod.startTime = ultimaDeclaracion.Length <= 0 ? infoWOrderExtend.ActualStartTime.ToString() : fechaFormatoISO;
                logger.LogInformation($"tiempos modificandos {prod}");
            }

            logger.LogInformation($"Procesando ultimo tiempo de declaracion");

            if (ultimaDeclaracion.Length <= 0)
            {
                logger.LogInformation($"Agregando ultimo tiempo de declaracion a la workorder [{infoOrder.NId}]");
                DateTime fecha1 = Convert.ToDateTime(prod.endTime);
                DateTimeOffset fechaOffset1 = new DateTimeOffset(fecha1);
                DateTimeOffset fechaOffsetSumada = fechaOffset1.AddHours(5);

                // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
                string fechaFormatoISO1 = fechaOffsetSumada.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                List<Params> props = new List<Params>();
                Params prop = new Params
                {
                    ParameterNId = "ultimaDeclaracion",
                    ParameterTargetValue = fechaFormatoISO1
                };
                props.Add(prop);

                await OrderService.AddWorkOrderParametersAsync(infoWOrder.Id, props.ToArray(), ct);

            }
            else
            {
                logger.LogInformation($"Actualizando ultimo tiempo de declaracion a la workorder [{infoOrder.NId}]");

                string Id = ultimaDeclaracion[0].Id;
                DateTime fecha = Convert.ToDateTime(prod.endTime);
                DateTimeOffset fechaOffset2 = new DateTimeOffset(fecha);
                DateTimeOffset fechaOffsetSumada = fechaOffset2.AddHours(5);

                // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
                string fechaFormatoISO2 = fechaOffsetSumada.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string Value = fechaFormatoISO2;

                await OrderService.UpdateWorkOrderParametersAsync(Id, Value, ct);


            }

            var woId = await SimaticWorkOrderCompletionService.CreateWoCompletionAsync(prod, ct);
            logger.LogInformation($"Order completion {prod.woChildrenId} send successfully with ID '{woId}'");

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

            if (resTemp.Count > 0) {
                res = (JObject) resTemp[0];
            }

            var lotes = string.Join("| ", prod.detail.Select(i => i.lotnumber));

            var newlog = new TransactionalLogModel
            {
                Planta = prod.location,
                Proceso = infoWOrderOp.Name,
                Maquina = prod.detail[0]?.binnumber ?? prod.materialConsumedActual[0]?.inventory[0]?.binnumber,
                Tipo = "COMPLETION",
                WorkOrders = infoWOrder.NId,
                Lotes = lotes,
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


            return woId;

            #endregion
        }

        public async Task<dynamic> CreateWoCompletionVinilosAsync(WorkOrderCompletionVinilosModel prod, CancellationToken ct)
        {

            logger.LogInformation($"A new WOCompletion data is received, analysing woChildrenId [{prod.woChildrenId}] ...");

            #region -- Create WoCompletion --
            logger.LogInformation($"preparacion de Datos a enviar a netsuit {prod}");
            string name = Convert.ToString(prod.woChildrenId);

            Order infoOrder = await OrderService.GetOrderByNameAsync(name, ct);
            WorkOrder infoWOrder = await OrderService.GetWorkOrderByNIdAsync(infoOrder.NId, false, ct);
            WorkOrderExtended infoWOrderExtend = await OrderService.GetWorkOrderExtendedByOrderNIdAsync(infoOrder.NId, false, ct);
            WorkOrderOperation infoWOrderOp = await OrderService.GetWorkOrderOperationBySequenceAsync(prod.startOperation.ToString(), ct);
            var ultimaDeclaracion = await OrderService.GetWorkOrderParameterUltimaDeclarion(infoWOrder.Id, ct);
            ProductionTime[] timeProd = await SimaticWorkOrderCompletionService.GetProductionTimeAsync(infoOrder.NId, ct);

            DateTime ultDec = ultimaDeclaracion.Length <= 0 ? new DateTime() : Convert.ToDateTime(ultimaDeclaracion[0].ParameterTargetValue);
            DateTimeOffset fechaOffset = new DateTimeOffset(ultDec);

            // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
            string fechaFormatoISO = fechaOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");


            logger.LogInformation($"Modificando tiempos del WoCompletion");

            if (timeProd.Length > 0)
            {
                logger.LogInformation($"prod Start: {timeProd[0].StartTime}");

                DateTime prodStart = Convert.ToDateTime(timeProd[0].StartTime);
                DateTimeOffset fechaOffsetProdstar = new DateTimeOffset(prodStart);
                string fechaFormatoISOProdstart = fechaOffsetProdstar.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                logger.LogInformation($"prod end: {timeProd[0].EndTime}");
                DateTime prodEnd = Convert.ToDateTime(timeProd[0].EndTime);
                DateTimeOffset fechaOffsetProdEnd = new DateTimeOffset(prodEnd);
                string fechaFormatoISOProdEnd = fechaOffsetProdEnd.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                logger.LogInformation($"prod end iso: {fechaFormatoISOProdEnd}");

                logger.LogInformation($"Adding operation time lms to payload");
                prod.startTime = ultimaDeclaracion.Length <= 0 ? fechaFormatoISOProdstart : fechaFormatoISO;
                prod.endTime = fechaFormatoISOProdEnd;
                logger.LogInformation($"tiempos modificandos lms{prod}");

            }
            else
            {
                logger.LogInformation($"Adding operation time actualStartTime to payload");
                prod.startTime = ultimaDeclaracion.Length <= 0 ? infoWOrderExtend.ActualStartTime.ToString() : fechaFormatoISO;
                logger.LogInformation($"tiempos modificandos {prod}");
            }

            logger.LogInformation($"Procesando ultimo tiempo de declaracion");

            if (ultimaDeclaracion.Length <= 0)
            {
                logger.LogInformation($"Agregando ultimo tiempo de declaracion a la workorder [{infoOrder.NId}]");
                DateTime fecha1 = Convert.ToDateTime(prod.endTime);
                DateTimeOffset fechaOffset1 = new DateTimeOffset(fecha1);
                DateTimeOffset fechaOffsetSumada = fechaOffset1.AddHours(5);

                // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
                string fechaFormatoISO1 = fechaOffsetSumada.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                List<Params> props = new List<Params>();
                Params prop = new Params
                {
                    ParameterNId = "ultimaDeclaracion",
                    ParameterTargetValue = fechaFormatoISO1
                };
                props.Add(prop);

                await OrderService.AddWorkOrderParametersAsync(infoWOrder.Id, props.ToArray(), ct);

            }
            else
            {
                logger.LogInformation($"Actualizando ultimo tiempo de declaracion a la workorder [{infoOrder.NId}]");

                string Id = ultimaDeclaracion[0].Id;
                DateTime fecha = Convert.ToDateTime(prod.endTime);
                DateTimeOffset fechaOffset2 = new DateTimeOffset(fecha);
                DateTimeOffset fechaOffsetSumada = fechaOffset2.AddHours(5);

                // Formatear la fecha en el formato deseado (ISO 8601 con zona horaria Z)
                string fechaFormatoISO2 = fechaOffsetSumada.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string Value = fechaFormatoISO2;

                await OrderService.UpdateWorkOrderParametersAsync(Id, Value, ct);


            }

            var woId = await SimaticWorkOrderCompletionService.CreateWoCompletionVinilosAsync(prod, ct);

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

            var lotes = string.Join("| ", prod.detail.Select(i => i.lotnumber));

            var newlog = new TransactionalLogModel
            {
                Planta = prod.location,
                Proceso = infoWOrderOp.Name,
                Maquina = prod.detail[0]?.binnumber ?? prod.materialConsumedActual[0]?.inventory[0]?.binnumber,
                Tipo = "COMPLETION",
                WorkOrders = infoWOrder.NId,
                Lotes = lotes,
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

            logger.LogInformation($"Order completion [{prod.woChildrenId}] send successfully with ID '{woId}'");

            return woId;

            #endregion
        }



    }
}

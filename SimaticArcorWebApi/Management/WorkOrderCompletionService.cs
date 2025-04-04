﻿using Endor.Core.Logger;
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
            WorkOrderOperation infoWOrderOp = await OrderService.GetWorkOrderOperationBySequenceAsync(prod.startOperation.ToString(), infoWOrder.Id, ct);
            WorkOrderExtended infoWOrderExtend = await OrderService.GetWorkOrderExtendedByOrderNIdAsync(infoWOrder.Id, false, ct);
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

            //if (prod.detail.Length > 0)
            //{
            //    HashSet<string> approvedMachines = new HashSet<string> { "A01", "A02","A03","A04","A05","G01", "K01", "K02", "K03", "K04", "K05", "K06", "K07", "K08", "K09", "K10", "K11", "K12", "K13", "T01","T02", "T03","H04","H05","H06","H07","S01","S02","F01","W01","E01","E02","P01","P02" };

            //    foreach (var item in prod.detail)
            //    {
            //        if ((item.bom != null && item.bom.StartsWith("3")) ||
            //            (approvedMachines.Contains(item.binnumber)))
            //        {
            //            item.inventorystatus = "APROBADO";
            //        }
            //    }
            //}

            logger.LogInformation($"Procesando ultimo tiempo de declaracion");
            await ProcesarUltimaDeclaracionAsync(infoOrder, infoWOrder, prod, ct);


            var woId = await SimaticWorkOrderCompletionService.CreateWoCompletionAsync(prod, ct);
            logger.LogInformation($"Order completion {prod.woChildrenId} - {infoWOrder.NId} send successfully with ID '{woId}'");

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

            JObject res = new JObject();

            if (resTemp.Count > 0)
            {
                res = (JObject)resTemp[0];
            }

            string maquina = "";

            if (prod.detail.Length > 0)
            {
                maquina = prod.detail[0]?.binnumber;
            }
            else if (prod.materialConsumedActual.Length > 0)
            {
                maquina = prod.materialConsumedActual[0]?.inventory[0]?.binnumber;
            }

            var lotes = string.Join("| ", prod.detail.Select(i => i.lotnumber));

            var newlog = new TransactionalLogModel
            {
                Planta = prod.location,
                Proceso = infoWOrderOp.Name,
                Maquina = maquina,
                Tipo = "COMPLETION",
                WorkOrders = infoWOrder.NId,
                Lotes = lotes,
                Descripcion = "",
                Trim = "",
                NTrim = "",
                Cortes = "",
                Payload = JsonConvert.SerializeObject(prod).ToString(),
            };


            newlog.ErrorMessage = res.ToString();
            newlog.ReasonStatus = res["error"]?["message"]?.ToString() != null ? res["error"]?["message"]?.ToString() : "Message Vacio";
            newlog.Succeced = res["isSuccess"]?.ToString() != null ? res["isSuccess"]?.ToString() : "False";
            newlog.Comando = "CreateWoCompletionAsync";
            newlog.ProgramaFuente = "OPCENTER";
            newlog.ProgramaDestino = "NETSUITE";
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
            WorkOrderExtended infoWOrderExtend = await OrderService.GetWorkOrderExtendedByOrderNIdAsync(infoWOrder.Id, false, ct);
            WorkOrderOperation infoWOrderOp = await OrderService.GetWorkOrderOperationBySequenceAsync(prod.startOperation.ToString(), infoWOrder.Id, ct);
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

            JObject res = new JObject();

            if (resTemp.Count > 0)
            {
                res = (JObject)resTemp[0];
            }

            var lotes = string.Join("| ", prod.detail.Select(i => i.lotnumber));

            string maquina = "";

            if (prod.detail.Length > 0)
            {
                maquina = prod.detail[0]?.binnumber;
            }
            else if (prod.materialConsumedActual.Length > 0)
            {
                maquina = prod.materialConsumedActual[0]?.inventory[0]?.binnumber;
            }

            var newlog = new TransactionalLogModel
            {
                Planta = prod.location,
                Proceso = infoWOrderOp.Name,
                Maquina = maquina,
                Tipo = "COMPLETION",
                WorkOrders = infoWOrder.NId,
                Lotes = lotes,
                Descripcion = "",
                Trim = "",
                NTrim = "",
                Cortes = "",
                Payload = JsonConvert.SerializeObject(prod).ToString(),
            };


            newlog.ErrorMessage = res.ToString();
            newlog.ReasonStatus = res["error"]?["message"]?.ToString() != null ? res["error"]?["message"]?.ToString() : "Message Vacio";
            newlog.Succeced = res["isSuccess"]?.ToString() != null ? res["isSuccess"]?.ToString() : "False";
            newlog.Comando = "CreateWoCompletionAsync";
            newlog.ProgramaFuente = "OPCENTER";
            newlog.ProgramaDestino = "NETSUITE";
            newlog.URL = this.UrlBase + this.nsURL;
            await TransactionalLogService.CreateTLog(newlog, ct);
            #endregion

            logger.LogInformation($"Order completion [{prod.woChildrenId}]-[{infoWOrder.NId}] send successfully with ID '{woId}'");

            return woId;

            #endregion
        }

        public async Task ProcesarUltimaDeclaracionAsync(Order infoOrder, WorkOrder infoWOrder, WorkOrderCompletionModel prod, CancellationToken ct)
        {
            if (infoOrder == null || infoWOrder == null)
            {
                logger.LogError("Error: Información de la orden no es válida.");
                return;
            }

            if (prod?.endTime == null)
            {
                logger.LogError($"Error: La fecha de finalización es nula para la orden [{infoOrder.NId}].");
                return;
            }

            try
            {
                // Convertir y formatear fecha
                DateTime fecha = Convert.ToDateTime(prod.endTime);
                DateTimeOffset fechaOffset = new DateTimeOffset(fecha).AddHours(5);
                string fechaFormatoISO = fechaOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                // 🔹 Primera consulta: verificamos si ya existe `ultimaDeclaracion`
                ProcessParameter[] ultimaDeclaracion = await OrderService.GetWorkOrderParameterUltimaDeclarion(infoWOrder.Id, ct);

                var declaracionExistente = ultimaDeclaracion?
                    .FirstOrDefault(p => p != null && p.ParameterNId == "ultimaDeclaracion");

                if (declaracionExistente != null)
                {
                    logger.LogInformation($"Última declaración ya existe, actualizando workorder [{infoOrder.NId}].");
                    await OrderService.UpdateWorkOrderParametersAsync(declaracionExistente.Id, fechaFormatoISO, ct);
                    return;
                }

                // 🔹 Esperamos un breve momento para dar oportunidad a que la otra ejecución termine
                await Task.Delay(500, ct);  // 500 ms de espera

                // 🔹 Segunda consulta: verificamos si ya se creó mientras esperábamos
                ultimaDeclaracion = await OrderService.GetWorkOrderParameterUltimaDeclarion(infoWOrder.Id, ct);
                declaracionExistente = ultimaDeclaracion?
                    .FirstOrDefault(p => p != null && p.ParameterNId == "ultimaDeclaracion");

                if (declaracionExistente != null)
                {
                    logger.LogInformation($"Última declaración ya fue creada mientras esperábamos. Actualizando en workorder [{infoOrder.NId}].");
                    await OrderService.UpdateWorkOrderParametersAsync(declaracionExistente.Id, fechaFormatoISO, ct);
                    return;
                }

                // 🔹 Si después de la segunda verificación aún no existe, la creamos
                logger.LogInformation($"Agregando última declaración a la workorder [{infoOrder.NId}].");

                var props = new List<Params>
        {
            new Params
            {
                ParameterNId = "ultimaDeclaracion",
                ParameterTargetValue = fechaFormatoISO
            }
        };

                await OrderService.AddWorkOrderParametersAsync(infoWOrder.Id, props.ToArray(), ct);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error procesando última declaración para la orden [{infoOrder.NId}]: {ex.Message}");
            }
        }
    }
}

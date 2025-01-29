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
using SimaticWebApi.Model.Custom.Counters;
using SimaticWebApi.Model.Custom.ProductionTime;
using SimaticArcorWebApi.Management;
using SimaticWebApi.Model.Custom.TransactionalLog;

namespace SimaticWebApi.Management
{

    public class TransactionalLogService : ITransactionaLog
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<TransactionalLogService> logger;

        public ISimaticService SimaticService { get; set; }
        public ISimaticTransactionalLogService SimaticTransactionalLogService { get; set; }


        #region Constructor

        public TransactionalLogService(ISimaticService simatic, ISimaticTransactionalLogService simaticTransactionalLogService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<TransactionalLogService>();

            SimaticService = simatic;
            SimaticTransactionalLogService = simaticTransactionalLogService;


        }
        #endregion

        public async Task<dynamic> CreateTlog(TransactionalLogModel log, CancellationToken ct)
        {
            logger.LogInformation($"Nuevo log recibido para procesar ...");
            var response = await SimaticTransactionalLogService.CreateTLog(log, ct);
            return response;
        }
    }
}

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


        public WorkOrderCompletionConsumoService(ISimaticService simatic, ISimaticWorkOrderCompletionService SimaticWOCompletionService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<WorkOrderCompletionConsumoService>();

            SimaticService = simatic;
            SimaticWorkOrderCompletionService = SimaticWOCompletionService;

        }

        public async Task<dynamic> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel prod)
        {

            string woId = string.Empty;
            string workwoId = string.Empty;
            string verb = string.Empty;

            logger.LogInformation($"A new Order data is received, analysing Order ID [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] for Final Material [{prod.materialConsumedActual[0].materialDefinitionId}] ...");


            #region -- Create / ReCreate Order & WorkOrder --

            // This operation always recreates an Order, this is IOInteroperability standard behaviour.
            woId = await SimaticWorkOrderCompletionService.CreateWoCompletionConsumoAsync(prod);
            logger.LogInformation($"Order completion only consum [{prod.woChildrenId}] - [{prod.woChildrenId}] created/recreated successfully with ID '{woId}'");
            return woId;

           

            #endregion
        }



    }
}

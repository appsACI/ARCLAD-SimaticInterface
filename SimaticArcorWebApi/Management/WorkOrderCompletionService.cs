using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;

using System;
using System.Threading;
using System.Threading.Tasks;

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
        public ISimaticOrderService SimaticOrderService { get; set; }


        public WorkOrderCompletionService(ISimaticService simatic, ISimaticWorkOrderCompletionService SimaticWOCompletionService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<WorkOrderCompletionService>();

            SimaticService = simatic;
            SimaticWorkOrderCompletionService = SimaticWOCompletionService;

        }

        public async Task CreateWoCompletionAsync(WorkOrderCompletionModel prod, CancellationToken ct)
        {

            string woId = string.Empty;
            string workwoId = string.Empty;
            string verb = string.Empty;

            logger.LogInformation($"A new Order data is received, analysing Order ID [{prod.woChildrenId}] WorkOrder [{prod.woChildrenId}] ...");


            #region -- Create / ReCreate Order & WorkOrder --
            //var infoWorkOrder = await SimaticOrderService.GetOrderByNameAsync(Convert.ToString(prod.woChildrenId), ct);

            // This operation always recreates an Order, this is IOInteroperability standard behaviour.
            woId = await SimaticWorkOrderCompletionService.CreateWoCompletionAsync(prod ,ct);

            // Further operations on the Order and WorkOrder
            if (!string.IsNullOrEmpty(woId))
            {
                logger.LogInformation($"Order completion [{prod.woChildrenId}] - [{prod.woChildrenId}] created/recreated successfully with ID '{woId}'");


            }
            else
            {
                logger.LogError($"Something went wrong, could not get just created Order [{prod.woChildrenId}] - [{prod.woChildrenId}], or it was not created at all! Check previous logs.");
                throw new Exception($"Could not create Order [{prod.woChildrenId}] - [{prod.woChildrenId}]!");
            }

            #endregion
        }



    }
}

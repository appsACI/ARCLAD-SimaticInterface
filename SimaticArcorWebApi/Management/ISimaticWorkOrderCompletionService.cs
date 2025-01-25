using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Custom.PalletReception;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Modules.DCMovement;

using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using HttpStatusCode = System.Net.HttpStatusCode;


namespace SimaticArcorWebApi.Management
{
    public interface ISimaticWorkOrderCompletionService
    {
        Task<dynamic> CreateWoCompletionAsync(WorkOrderCompletionModel WoCompletionData, CancellationToken ct);

        Task<dynamic> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel WoCompletionConsumoData);

        Task<dynamic> GetProductionTimeAsync(string WorkOrderId, CancellationToken token);

        Task<dynamic> CreateWoCompletionVinilosAsync(WorkOrderCompletionVinilosModel prod, CancellationToken ct);
    }
}

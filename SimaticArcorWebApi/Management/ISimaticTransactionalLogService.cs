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
using SimaticWebApi.Model.Custom.TransactionalLog;

namespace SimaticWebApi.Management
{
    public interface ISimaticTransactionalLogService
    {
        Task<dynamic> CreateTLog(TransactionalLogModel log, CancellationToken ct);
    }
}

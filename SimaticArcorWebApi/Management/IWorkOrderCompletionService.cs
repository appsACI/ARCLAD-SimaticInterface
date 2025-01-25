using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
    public interface IWorkOrderCompletionService
    {
        Task<dynamic> CreateWoCompletionAsync(WorkOrderCompletionModel prod, CancellationToken ct);

        Task<dynamic> CreateWoCompletionVinilosAsync(WorkOrderCompletionVinilosModel prod, CancellationToken ct);
    }
}

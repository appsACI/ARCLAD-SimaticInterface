﻿using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
    public interface IWorkOrderCompletionConsumoService
    {
        Task<dynamic> CreateWoCompletionConsumoAsync(WorkOrderCompletionConsumoModel prod, CancellationToken ct);
    }
}

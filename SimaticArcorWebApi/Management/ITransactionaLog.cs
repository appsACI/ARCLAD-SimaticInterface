using SimaticWebApi.Model.Custom.TransactionalLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticWebApi.Management
{
    public interface ITransactionaLog
    {
        Task<dynamic> CreateTlog(TransactionalLogModel log, CancellationToken ct);
    }
}

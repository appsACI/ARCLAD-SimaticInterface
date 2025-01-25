using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom.BulkReception;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;

namespace SimaticArcorWebApi.Management
{
    public interface IBulkReceptionService
    {
        Task TruckReceiveProcessingAsync(TruckReceiveRequest req, CancellationToken ct);

        //Task<IList<BulkReception>> GetBulkReceptionByLotAsync(string lotNId, CancellationToken ct);

        Task ConfirmBulkReceptionAsync(TruckReceiveRequest req, CancellationToken ct);

        //Task UpdateLotAsync(MTURequest req, CancellationToken ct);

        //Task CreateOrUpdateMTUProperties(string id, MTURequestMaterialLotProperty[] requestProperties,
        //    CancellationToken token);
    }
}

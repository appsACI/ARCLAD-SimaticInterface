using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticWebApi.Model.Custom.PrintLabel;

namespace SimaticArcorWebApi.Management
{
    public interface IMTUService
    {
        Task UpdateLotAsync(MTURequest req, CancellationToken ct);
        Task DescountQuantity(MTUDescount req, CancellationToken ct);
        Task PrintLabel(PrintModel req, CancellationToken ct);
        Task CreateOrUpdateMTUProperties(string id, MTURequestMaterialLotProperty[] requestProperties, CancellationToken token);
        Task MoveMTUAsync(MTURequest req, CancellationToken ct);
        Task LotMovementJDE(MTURequest req, CancellationToken ct);
        Task AssignedUnassign(MTUAssignedUnassign req, CancellationToken ct);

        Task<IList<MTUPropiedadesDefectos>> GetPropertiesDefectos(string parameterId, CancellationToken ct);
    }
}
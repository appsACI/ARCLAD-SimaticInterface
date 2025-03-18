using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticWebApi.Model.Custom.PrintLabel;
using SimaticWebApi.Model.Custom.RDL;

namespace SimaticArcorWebApi.Management
{
    public interface IMTUService
    {
        Task UpdateLotAsync(MTURequest req, CancellationToken ct);
        Task UpdateLotVinilosAsync(MTURequest[] req, CancellationToken ct);
        Task DescountQuantity(MTUDescount req, CancellationToken ct);
        Task PrintLabel(PrintModel req, CancellationToken ct);
        Task CreateOrUpdateMTUProperties(string id, MTURequestMaterialLotProperty[] requestProperties, CancellationToken token);
        Task MoveMTUAsync(MTURequest req, CancellationToken ct);
        Task LotMovementJDE(MTURequest req, CancellationToken ct);
        Task AssignedUnassign(MTUAssignedUnassign req, CancellationToken ct);

        Task<IList<MTUPropiedadesDefectos>> GetPropertiesDefectos(string parameterId, CancellationToken ct);
        Task<dynamic> GetLabelHistory(ReimpresionFilters req, CancellationToken ct);
        Task<dynamic> GetLotePadreProp(string NId, CancellationToken token);
        #region RDL
        Task<string> CreateRequirement(CreateRequirementModel req, CancellationToken ct);
        Task<string> UpdateRequirement(CreateRequirementModel req, CancellationToken ct);

        Task<string> CreateRequirementCorte(CreateRequirementModelCorte req, CancellationToken ct);
        Task<string> UpdateRequirementLength(UpdateLengthModel req, CancellationToken ct);
        Task<string> ConsultaRequirement(string Lote, string Nid, CancellationToken ct);

        #endregion
    }
}
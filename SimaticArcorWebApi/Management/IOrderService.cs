using SimaticArcorWebApi.Model.Custom;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticWebApi.Model.Custom.Counters;

namespace SimaticArcorWebApi.Management
{
    public interface IOrderService
    {
        Task ProcessNewOrderAsync(ProductionRequest bom, CancellationToken token);

        Task ProcessNewOrderV2Async(ProductionRequest prod, CancellationToken ct);

        Task ProcessNewOperationAsync(WorkOrderOperationRevisionRollosRequest prod, CancellationToken ct);

        Task ChangeStatusAndTimeOrden(StatusAndTimeOrder prod, CancellationToken ct);

        Task<IList<WorkOrderOperationParameterSpecification>> GetParametersRevisionTrim(string orderId, CancellationToken ct);

        Task<IList<WorkOrderOperationParameterSpecification>> GetParametersRevisionMTU(string orderId, CancellationToken ct);

        Task<IList<MaterialTrackingUnitProperty>> GetParametersMTU(string NId, CancellationToken ct);

        Task<WorkOrderOperationParameterSpecification> UpdateCounterTrimCorte(string parameterId,string cantCortes, CancellationToken ct);

        Task UpdateChecksRevisionOperationsAsync(WorkOrderOperationRevisionRequest prod, CancellationToken ct);

        Task AddMaterialOperationEmpaqueCorteHojasAsync(MaterialOperationEmpaqueCorteHojas prod, CancellationToken ct);

        Task UpdateWorkOrderCounters(Counters req, CancellationToken ct);
    }
}

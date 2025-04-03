using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticWebApi.Model.Custom.Counters;
using SimaticWebApi.Model.Custom.Reproceso;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace SimaticArcorWebApi.Management
{
    public interface ISimaticOrderService
    {
        Task<string> CreateOrderAsync(ProductionRequest orderData, Material material, IOOrderUserField[] userFields, IOOrderOperation[] orderOperations, CancellationToken ct);

        Task<string> UpdateOrderAsync(string id, string name, string description, DateTime? actualStartTime, DateTime? ActualEndTime,
          string equipment, string materialId, string materialRevision, DateTime startTime, DateTime endTime, string stateMachine,
          decimal quantity, string unitOfMeasure, string type, CancellationToken ct);

        Task<string> UpdateWorkOrderExtendedV2(string id, DateTime startTime, DateTime endTime, decimal quantity, string unitOfMeasure, CancellationToken ct);

        Task<string> UpdateWorkOrderOperationExtended(string id, string actualEquipmentNId, string description, bool isInExecutionPropagation, string name, string operationNId, string operationRevision, bool resetProperty, int sequence, CancellationToken ct);

        Task<string> UpdateWorkOrderOperationEquipmentRequirement(string id, string equipmentNId, string equipmentGroupNId, bool isMain, string plannedEquimentNId, string requierementTag, int sequence, CancellationToken ct);

        Task<dynamic> GetOrderByNIdAsync(string id, bool throwException, CancellationToken token);

        Task<dynamic> GetWorkOrderByNIdAsync(string nid, bool throwException, CancellationToken token);

        Task<dynamic> GetWorkOrderOperationAsync(Guid WorkOrderId, CancellationToken token);

        Task<dynamic> GetWorkOrderApplicationLogs(string id, CancellationToken token);

        Task<dynamic> GetApplicationLogMessages(IList<string> id, CancellationToken token);

        Task<dynamic> GetWorkOrderExtendedByOrderNIdAsync(string id, bool throwException, CancellationToken token);

        Task<dynamic> GetWorkOrderByIdAsync(string id, bool throwException, CancellationToken token);

        Task<dynamic> GetOrderUserFields(string currentOrder, CancellationToken ct);

        Task UpdateOrderUserField(string id, string value, CancellationToken token);

        Task CreateOrderUserField(string id, string key, string value, CancellationToken token);

        Task SetOrderStatusAsync(string id, string verb, CancellationToken token);

        Task SetWorkOrderActualTimeStart(string id, DateTime? time, CancellationToken token);

        Task SetWorkOrderActualTimeEnd(string id, DateTime? time, CancellationToken token);
        Task<dynamic> GetWorkOrderExtendedByWorkOrderIdAsync(Guid id, bool throwException, CancellationToken token);

        Task ClearWorkOrderActualTime(string id, DateTime plannedInit, DateTime plannedFinish, CancellationToken token);

        Task<dynamic> GetWorkOrderOperationBySequenceAsync(string secuence,string WOId, CancellationToken token);

        Task<dynamic> GetWorkOrderUserFieldsByID(string id, CancellationToken token);

        Task UpdateWorkOrderUserField(string id, string value, CancellationToken token);

        Task SetWorkOrderStatusAsync(string id, string verb, CancellationToken token);

        Task SetWorkOrderOperationAsync(WorkOrderOperationRevisionRequest prod, string verb, CancellationToken token);

        Task SetWorkOrderOperationStatusAsync(string id, string verb, CancellationToken token);

        Task DeleteOrderAsync(string id, CancellationToken token);

        Task DeleteOrderOperationAsync(Guid id, CancellationToken token);

        Task DeleteWorkOrderAsync(string id, CancellationToken token);

        Task<List<Guid>> CreateWorkOrderAsync(Order order, Material material, CancellationToken ct);

        Task<string> UpdateWorkOrderParametersRollosClientes(string ordenVenta, string Ancho, string Cliente, string NroRollos, string M2, string Notas, CancellationToken ct);

        Task<dynamic> GetWorkOrderOperationRevision(string woId, CancellationToken ct);

        Task<IList<MaterialTrackingUnitProperty>> GetMaterialTrackingUnitProperty(string NId, CancellationToken token);

        Task<dynamic> GetWorkOrderOperationParameterSpecificationRollos(string woId, CancellationToken ct);

        Task<dynamic> GetWorkOrderOperationParameterSpecificationRollosWithOutDeclarar(string woId, CancellationToken ct);

        Task<dynamic> GetWorkOrderOperationParameterSpecificationId(string Id, CancellationToken ct);

        Task SetWorkOrderOperationParametersSpecificationAsync(WorkOrderOperationRevisionRollosRequest prod, string verb, CancellationToken token);

        Task UpdateWorkOrderOperationParameterRequirementAsync(IList<UpdateParameterSpecification> prod, string verb, CancellationToken token);

        Task UpdateCounterTrimCorteAsync(UpdateParameterSpecification prod, string verb, CancellationToken token);

        Task<dynamic> GetWorkOrderOperationAny(string woId, string OperationNId, CancellationToken ct);

        Task<dynamic> GetWorkOrderOperationMaterialRequirementAndMaterial(string Id, string MaterialNId, CancellationToken ct);

        Task AddWorkOrderOperationMaterialRequirementsToWorkOrderOperation(IList<MaterialRequirement> prod, string workOrderOperationId, string verb, CancellationToken token);

        Task<dynamic> GetWorkOrderParametersAsync(string id, CancellationToken token);

        Task<dynamic> GetWorkOrderParameterUltimaDeclarion(string id, CancellationToken token);

        Task AddWorkOrderParametersAsync(string woId, Params[] parameters, CancellationToken token);

        Task<dynamic> UpdateWorkOrderParametersAsync(string propId, string propValue, CancellationToken token);

        Task DeleteWorkOrderParameter(string id, CancellationToken token);

        Task<HttpResponseMessage> PostToRTDSWriteAsync(RTDS requestBody, CancellationToken ct);

        Task<string> AddMaterialReprosesoWorkOrderAsync(WorkOrderOperationMaterialRequirement[] materiales, string workOrderOperationId, CancellationToken ct);

        Task<string> AddMaterialReprosesoOrderAsync(OrderOperationMaterialRequirement[] materiales, string orderOperationId, CancellationToken ct);

        Task<dynamic> GetOrderOperationAsync(Guid WorkOrderId, CancellationToken token);

        Task<string> AddWorkOrderPriority(string id, int priority, CancellationToken ct);

        Task<dynamic> GetOrderByNameAsync(string id, CancellationToken token);
    }
}

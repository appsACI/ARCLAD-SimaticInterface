using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticWebApi.Model.Simatic.WorkOrder;
using System;
using System.Collections.Generic;

namespace SimaticArcorWebApi.Model.Simatic.WorkOrder
{
    public class WorkOrder
    {
        public string Id { get; set; }
        public string AId { get; set; }
        public bool IsFrozen { get; set; }
        public int ConcurrencyVersion { get; set; }
        public int IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string EntityType { get; set; }
        public string OptimisticVersion { get; set; }
        public object ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public string NId { get; set; }
        public string TemplateNId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public object BillOfMaterialsNId { get; set; }
        public object BillOfMaterialsRevision { get; set; }
        public object BillOfOperationsNId { get; set; }
        public object BillOfOperationsRevision { get; set; }
        public object WorkCenterNId { get; set; }
        public object EquipmentNId { get; set; }
        public WorkOrderStatus Status { get; set; }
        public WorkOrderQuantity Quantity { get; set; }
        public WorkOrderRollos Rollos { get; set; }
    }

    public class WorkOrderOperation
    {
        public string Id { get; set; }
        public string AId { get; set; }
        public bool IsFrozen { get; set; }
        public int ConcurrencyVersion { get; set; }
        public int IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string EntityType { get; set; }
        public string OptimisticVersion { get; set; }
        public object ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public string NId { get; set; }
        public string TemplateNId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Sequence { get; set; }
        public string OperationNId { get; set; }
        public string OperationRevision { get; set; }
        public string EquipmentNId { get; set; }
        public WorkOrderOperationStatus Status { get; set; }
    }

    public class WorkOrderOperationParameterSpecification
    {
        public string Id { get; set; }
        public string AId { get; set; }
        public bool IsFrozen { get; set; }
        public int ConcurrencyVersion { get; set; }
        public int IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string EntityType { get; set; }
        public string OptimisticVersion { get; set; }
        public object ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public string ParameterNId { get; set; }
        public string ParameterName { get; set; }
        public string ParameterDescription { get; set; }
        public string ParameterTargetValue { get; set; }
        public string ParameterType { get; set; }
        public string ParameterUoMNId { get; set; }
        public string TaskNId { get; set; }
        public string TaskParameterNId { get; set; }
        public string WorkProcessNId { get; set; }
        public string WorkProcessVariableNId { get; set; }
        public string ParameterLimitLow { get; set; }
        public string ParameterToleranceLow { get; set; }
        public string ParameterToleranceHigh { get; set; }
        public string ParameterLimitHigh { get; set; }
        public string ProcessDefinitionNId { get; set; }
        public string ProcessDefinitionRevision { get; set; }
        public string ParameterActualValue { get; set; }
        public string EquipmentNId { get; set; }
        public string WorkOrderOperation_Id { get; set; }
        public IList<MaterialTrackingUnitProperty> MaterialTrackingUnitProperty { get; set; } = new List<MaterialTrackingUnitProperty>();
    }

    public class UpdateParameterSpecification
    {
        public string Id { get; set; }
        public string ParameterValue { get; set; }
        public string ParameterLimitLow { get; set; }
        public string ParameterToleranceLow { get; set; }
        public string ParameterToleranceHigh { get; set; }
        public string ParameterLimitHigh { get; set; }
        public object ParameterActualValue { get; set; }
        public string EquipmentNId { get; set; }
        public string ParameterNId { get; set; }
        public string WorkProcessVariableNId { get; set; }
        public string TaskParameterNId { get; set; }
    }

    public class ProcessParameter
    {
        public string Id { get; set; }
        public string ParameterNId { get; set; }
        public string ParameterTargetValue { get; set; }
        public string ParameterLimitLow { get; set; }
        public string ParameterToleranceLow { get; set; }
        public string ParameterToleranceHigh { get; set; }
        public string ParameterLimitHigh { get; set; }
        public bool IsLimitsInPercentage { get; set; }
        public string ParameterUoMNId { get; set; }
    }

    public class Quantity
    {
        public string UoMNId { get; set; }
        public decimal QuantityValue { get; set; }
    }

    public class SegregationTag
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string Id { get; set; }
    }

    public class MaterialResponse
    {
        public Guid Id { get; set; }
        public string EquipmentNId { get; set; }
        public bool IsFrozen { get; set; }
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public string BoMNId { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public DateTime CreatedOn { get; set; }
        public string BomRevision { get; set; }
        public string MaterialName { get; set; }
        public int Sequence { get; set; }
        public string Direction { get; set; }
        public string Usage { get; set; }
        public string EquipmentGraphNId { get; set; }
        public string RequirementTag { get; set; }
        public string Tolerance { get; set; }
        public string BoMItemNId { get; set; }
        public string WorkOrderOperationId { get; set; }
        public Quantity Quantity { get; set; }
        public List<SegregationTag> SegregationTags { get; set; }
    }

    public class MaterialRequirement
    {
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public Quantity Quantity { get; set; }
        public int Sequence { get; set; }
        public string Usage { get; set; }
        public string Direction { get; set; }
        public string EquipmentNId { get; set; }
        public string EquipmentGraphNId { get; set; }
    }

    public class MaterialsQuantity
    {
        public string materialFinal { get; set; }
        public decimal quantity { get; set; }
    }

    public class MaterialOperationEmpaqueCorteHojas
    {
        public string workOrderOperationId { get; set; }
        public List<MaterialsQuantity> materialsQuantity { get; set; }
    }

}
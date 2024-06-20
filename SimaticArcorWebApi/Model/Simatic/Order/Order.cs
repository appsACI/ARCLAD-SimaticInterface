using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic.Order
{
    public class Order
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
        public DateTime? ActualEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public string Description { get; set; }
        public string EquipmentNId { get; set; }
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public string Name { get; set; }
        public string NId { get; set; }
        public string Type { get; set; }
        public DateTime PlannedEndTime { get; set; }
        public DateTime PlannedStartTime { get; set; }
        public OrderQuantity Quantity { get; set; }
        public OrderStatus Status { get; set; }
        public Operation[] Operations { get; set; }
        public IOParametersRollosClientes[] ParametersRollosClientes { get; set; }
    }

    public class Operation
    {
        public Guid Id { get; set; }
        public Guid AId { get; set; }
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
        public string OperationNId { get; set; }
        public string OperationRevision { get; set; }
        public string Description { get; set; }
        public string Sequence { get; set; }
        public string ExecutionPropagation { get; set; }
        public bool AvailableForWorkOrderEnrichment { get; set; }
        public Guid Order_Id { get; set; }
    }

    /// <summary>
    /// Interoperability IOCreateOrder OrderOperation
    /// </summary>
    public class IOOrderOperation
    {
        public string NId { get; set; }
        public string OperationNId { get; set; }
        public string Sequence { get; set; }
        public string AvailableForWorkOrderEnrichment { get; set; }
        public IOOrderOperationEquipmentRequirements[] OrderOperationEquipmentRequirements { get; set; }
        public IOOrderOperationMaterialRequirements[] OrderOperationMaterialRequirements { get; set; }
        public IOOrderOperationParameterRequirements[] OrderOperationParameterRequirements { get; set; }


    }

    public class IOOrderOperationEquipmentRequirements
    {
        public string EquipmentNId { get; set; }
        public string Sequence { get; set; }
    }

    public class IOOrderOperationMaterialRequirements
    {
        public string MaterialNId { get; set; }
        public IOQuantity Quantity { get; set; }
        public string Sequence { get; set; }
        public string Direction { get; set; }
    }

    public class IOOrderOperationParameterRequirements
    {
        public string ParameterNId { get; set; }
        public string ParameterName { get; set; }
        public string ParameterTargetValue { get; set; }
        public string ParameterType { get; set; }
        public string ParameterUoMNId { get; set; }
    }

    //public class IOOrderOperationInventoryDetail
    //{
    //    public string Lote { get; set; }
    //}

    public class IOQuantity
    {
        public decimal QuantityValue { get; set; }
        public string UoMNId { get; set; }
    }

    public class IOParametersRollosClientes
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string UoM { get; set; }
        public string Value { get; set; }
    }

    public class StatusAndTimeOrder
    {
        public string WOId { get; set; }
        public string Status { get; set; }
        public DateTime Time { get; set; }
    }

    public class RTDS
    {
        public string OrderID { get; set; }
        public string Operario { get; set; }
        public string Product { get; set; }
        public string LineaProduct { get; set; }
        public string Lote { get; set; }
        public string VelocidadNominal { get; set; }
    }

    public class Specification
    {
        public string SP_VALUE { get; set; }
        public string DESCRIPTION { get; set; }
        public string FR_VALUE { get; set; }
        public int STYPE { get; set; }
        public int LC { get; set; }
        public int CONTEXT { get; set; }
        public string CONTEXT_KEY1 { get; set; }
        public string CONTEXT_KEY2 { get; set; }
        public string CONTEXT_KEY3 { get; set; }
        public string CONTEXT_KEY4 { get; set; }
        public string CONTEXT_KEY5 { get; set; }
    }
}

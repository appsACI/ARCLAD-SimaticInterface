using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.Reproceso
{
    public class WorkOrderOperationMaterialRequirement
    {
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public QuantityReproceso Quantity { get; set; }
        public int Sequence { get; set; }
        public string Usage { get; set; }
        public string Direction { get; set; }
        public string EquipmentNId { get; set; }
        public string EquipmentGraphNId { get; set; }

    }

    public class OrderOperationMaterialRequirement
    {
        public string MaterialNId { get; set; }
        public string MaterialRevision { get; set; }
        public QuantityReproceso Quantity { get; set; }
        public int Sequence { get; set; }
        public string Usage { get; set; }
        public string Direction { get; set; }
        //public string EquipmentNId { get; set; }
        //public string EquipmentGraphNId { get; set; }

    }


    public class QuantityReproceso
    {
        public decimal QuantityValue { get; set; }
        public string UoMNId { get; set; }
    }

    public class WorkOrderReproceso
    {
        public string WorkOrderOperationId { get; set; }
        public WorkOrderOperationMaterialRequirement[] WorkOrderOperationMaterialRequirements { get; set; }
    }

    public class OrderReproceso
    {
        public string OrderOperationId { get; set; }
        public OrderOperationMaterialRequirement[] OrderOperationMaterialRequirements { get; set; }
    }

}

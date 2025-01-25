using SimaticArcorWebApi.Model.Simatic.Order;
using System;

namespace SimaticArcorWebApi.Model.Custom.WorkOrderCompletion
{
    public class WorkOrderCompletionConsumoModel
    {
        public int woChildrenId { get; set; }
        public string location { get; set; }
        public DateTime publishedDate { get; set; }
        public int startOperation { get; set; }
        public int endOperation { get; set; }
        public string memo { get; set; }
        public bool flagTrim { get; set; }
        public MaterialConsumedActualForWorkOrderCompletionConsumo[] materialConsumedActual { get; set; }

    }

    public class MaterialConsumedActualForWorkOrderCompletionConsumo
    {
        public string materialDefinitionId { get; set; }
        public string width { get; set; }
        public decimal quantity { get; set; }
        public InventoryForWorkOrderCompletionConsumo[] inventory { get; set; }
    }

    public class InventoryForWorkOrderCompletionConsumo
    {
        public string materialLotId { get; set; }
        public string materialLotStatus { get; set; }
        public decimal quantity { get; set; }
        public string binnumber { get; set; }
    }

}





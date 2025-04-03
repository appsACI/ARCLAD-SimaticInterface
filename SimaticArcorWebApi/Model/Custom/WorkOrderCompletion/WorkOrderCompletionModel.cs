using SimaticArcorWebApi.Model.Simatic.Order;
using System;

namespace SimaticArcorWebApi.Model.Custom.WorkOrderCompletion
{
    public class WorkOrderCompletionModel
    {
        public int woChildrenId { get; set; }
        public string location { get; set; }
        public DateTime publishedDate { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public int production { get; set; }
        public decimal? quantity { get; set; }
        public int startOperation { get; set; }
        public int endOperation { get; set; }
        public string memo { get; set; }
        public int employee { get; set; }
        public bool completed { get; set; }
        public bool flagTrim { get; set; }
        public bool isbackflush { get; set; }
        public int employeeComplement { get; set; }
        public DetailsForWorkOrderCompletion[] detail { get; set; }
        public MaterialConsumedActualForWorkOrderCompletion[] materialConsumedActual { get; set; }
        public ScrapForWorkOrderCompletion[] scrap { get; set; }

    }

    public class DetailsForWorkOrderCompletion
    {
        public string lotnumber { get; set; }
        public string binnumber { get; set; }
        public decimal? quantity { get; set; }
        public string versionLote { get; set; }
        public string width { get; set; }
        public string length { get; set; }
        public string trueLength { get; set; }
        public string parentLot { get; set; }
        public string bom { get; set; }
        public string expirationDate { get; set; }
        public string memo { get; set; }
        public string empates { get; set; } = "0";
        public string inventorystatus { get; set; } = "APROBADO";

    }

    public class MaterialConsumedActualForWorkOrderCompletion
    {
        public string materialDefinitionId { get; set; }
        public string width { get; set; }
        public decimal? quantity { get; set; }
        public InventoryForWorkOrderCompletion[] inventory { get; set; }
    }

    public class InventoryForWorkOrderCompletion
    {
        public string materialLotId { get; set; }
        public string materialLotStatus { get; set; }
        public decimal? quantity { get; set; }
        public string binnumber { get; set; }
    }

    public class ScrapForWorkOrderCompletion
    {
        public decimal? quantityScrap { get; set; }
        public string width { get; set; }
        public string conceptScrap { get; set; }
    }

}





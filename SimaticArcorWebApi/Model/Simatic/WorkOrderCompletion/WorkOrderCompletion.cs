using SimaticArcorWebApi.Model.Simatic;
using System;

namespace SimaticWebApi.Model.Simatic.WorkOrderCompletion
{
    public class IOWorkOrderCompletion
    {
        public int WoId { get; set; }
        public int WoChildrenId { get; set; }
        public string Location { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Production { get; set; }
        public decimal Quantity { get; set; }
        public int StartOperation { get; set; }
        public int EndOperation { get; set; }
        public string Memo { get; set; }
        public int Employee { get; set; }
        public bool Completed { get; set; }
        public int EmployeeComplement { get; set; }
        public IODetailsForWorkOrderCompletion[] Detail { get; set; }
        public IOMaterialConsumedActualForWorkOrderCompletion[] MaterialConsumedActual { get; set; }
        public IOScrapForWorkOrderCompletion[] Scrap { get; set; }

    }

    public class IODetailsForWorkOrderCompletion
    {
        public string Lotnumber { get; set; }
        public string Binnumber { get; set; }
        public int Quantity { get; set; }
        public string VersionLote { get; set; }
        public string Length { get; set; }
        public string TrueLength { get; set; }
        public string ParentLot { get; set; }
        public string Bom { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Memo { get; set; }
    }

    public class IOMaterialConsumedActualForWorkOrderCompletion
    {
        public string MaterialDefinitionId { get; set; }
        public string Quantity { get; set; }
        public IOInventoryForWorkOrderCompletion[] Inventory { get; set; }
    }

    public class IOInventoryForWorkOrderCompletion
    {
        public string MaterialLotID { get; set; }
        public string MaterialLotStatus { get; set; }
        public string Quantity { get; set; }
        public string Binnumber { get; set; }
    }


    public class IOScrapForWorkOrderCompletion
    {
        public string QuantityScrap { get; set; }
        public string ConceptScrap { get; set; }
    }


}





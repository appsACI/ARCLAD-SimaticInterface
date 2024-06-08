using SimaticArcorWebApi.Management;
using System;

namespace SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit
{
    public class MTURequest
    {
        public string Id { get; set; }
        public string MaterialDefinitionID { get; set; }
        public string Status { get; set; }
        public MTURequestLocation Location { get; set; }
        public MTURequestQuantity Quantity { get; set; }
        public MTURequestMaterialLotProperty[] MaterialLotProperty { get; set; }
    }

    public class MTUDescount
    {
        public string WorkOrderNId { get; set; }
        public MtuInfo[] MtuInfo { get; set; }
    }

    public class MtuInfo
    {
        public string MtuNId { get; set; }
        public double Quantity { get; set; }
    }

    public class TrazabilidadMPModel
    {
        public string LotId { get; set; }
        public decimal Quantity { get; set; }
        public string User { get; set; }
        public string UoM { get; set; }
        public DateTime Time { get; set; }
        public string EquipmentNId { get; set; }
        public string MaterialNId { get; set; }
        public string WorkOrderNId { get; set; }
        public string WoOperationNId { get; set; }
        public MTUTrazabilidadData[] MtuData { get; set; }
    }

    public class MTUTrazabilidadData
    {
        public string MtuNId { get; set; }
        public decimal Quantity { get; set; }
        public string UoM { get; set; }
        public string MaterialNId { get; set; }

    }



    public class TrazabilidadMP
    {
        public string OperationType { get; set; }
        public DateTime Timestamp { get; set; }
        public string ReasonCode { get; set; }
        public string User { get; set; }
        public string SourceEquipmentNId { get; set; }
        public string DestinationEquipmentNId { get; set; }
        public string SourceMaterialNId { get; set; }
        public string SourceMaterialRevision { get; set; }
        public string SourceLotNId { get; set; }
        public string SourceMTUNId { get; set; }
        public decimal SourceQuantity { get; set; }
        public string SourceUoMNId { get; set; }
        public string DestinationMaterialNId { get; set; }
        public string DestinationMaterialRevision { get; set; }
        public string DestinationLotNId { get; set; }
        public string DestinationMTUNId { get; set; }
        public decimal DestinationQuantity { get; set; }
        public string DestinationUoMNId { get; set; }
        public string SourceWorkOrderNId { get; set; }
        public string SourceWorkOrderOperationNId { get; set; }
        public string DestinationWorkOrderNId { get; set; }
        public string DestinationWorkOrderOperationNId { get; set; }
    }
}
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using System;

namespace SimaticArcorWebApi.Model.Custom
{
    public class ProductionRequest
    {
        public string Id { get; set; }
        public string Order { get; set; }
        public string WorkOrder { get; set; }
        public string OrderType { get; set; }
        public string Location { get; set; }
        public decimal Quantity { get; set; }
        public string AssemblyItem { get; set; }
        public string BomId { get; set; }
        public string Department { get; set; }
        public string ManufacturingRouting { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string FechaEntrega { get; set; }
        public string ARObservaciones { get; set; }
        public string Status { get; set; }
        public bool FlagTrim { get; set; }
        public string Customer { get; set; }
        public string OrdenVenta { get; set; }
        public string MadeInColombia { get; set; }
        public string InternalItem { get; set; }
        public string Memo { get; set; }
        public string Shelflife { get; set; }
        public int Priority { get; set; }
        public ProductionRequestParameters[] Parameters { get; set; }
        public ProductionRequestOperations[] Operations { get; set; }
        public ParametersRollosClientes[] ParametersRollosClientes { get; set; }
    }

    public class ProductionRequestParameters
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public string UoM { get; set; }
        public string Dimension { get; set; }
    }

    public class ProductionRequestOperations
    {
        public string Id { get; set; }
        public string Sequence { get; set; }
        public string Name { get; set; }
        public string Asset { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string AnchoUtil { get; set; }
        public ProductionRequestOperationAlternativeAssets[] AlternativeAssets { get; set; }
        public ProductionRequestOperationParameters[] Parameters { get; set; }
        //public ProductionRequestOperationInventoryDetail[] InventoryDetail { get; set; }
    }

    public class ProductionRequestOperationAlternativeAssets
    {
        public string Code { get; set; }
    }

    public class ProductionRequestOperationParameters
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public string UoM { get; set; }
    }


    public class ProductionRequestOperationInventoryDetail
    {
        public string Lote { get; set; }
    }

    public class ParametersRollosClientes
    {
        public ProductionRequestOperationParameters[] Rollo { get; set; }
    }

    public class WorkOrderOperationRevisionRequest
    {
        //{
        //    "Id": "c63d8483-34c1-ee11-b835-020017035491",
        //    "WorkOrderOperations": [
        //        {
        //            "NId": "5829",
        //            "Name": "Prueba",
        //            "Description": "Descripcion Prueba",
        //            "Sequence": 41,
        //            "OperationNId": "CORTE",
        //            "OperationRevision": "1"
        //        }
        //    ]
        //}

        public string Id { get; set; }
        public ProcessParameter[] ProcessParameters { get; set; }
    }

    public class WorkOrderOperationRevisionRollosRequest
    {
        //{
        //    "Id": "c63d8483-34c1-ee11-b835-020017035491",
        //    "WorkOrderOperations": [
        //        {
        //            "NId": "5829",
        //            "Name": "Prueba",
        //            "Description": "Descripcion Prueba",
        //            "Sequence": 41,
        //            "OperationNId": "CORTE",
        //            "OperationRevision": "1"
        //        }
        //    ]
        //}

        public string Id { get; set; }
        public ProcessParameterRollos[] ProcessParameters { get; set; }
    }
}





using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using System;

namespace SimaticArcorWebApi.Model.Custom
{

    public class PQRLMSRequest
    {
        public string Title { get; set; }
        public string EquipmentNId { get; set; }
        public string IncommingMessage { get; set; }
        public string Tipo { get; set; }
    }

    public class PQRLMSResponse
    {
        public string message { get; set; }
        public IncommingMessage response { get; set; }
    }

    public class IncommingMessage
    {
        public string supportCaseId { get; set; }
    }
}





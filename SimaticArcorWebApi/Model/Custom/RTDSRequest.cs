using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using System;

namespace SimaticArcorWebApi.Model.Custom
{
    public class RTDSRequest
    {
        public string OrderID { get; set; }
        public string Operario { get; set; }
        public string Product { get; set; }
        public string LineaProduct { get; set; }
        public string Lote { get; set; }
        public string VelocidadNominal { get; set; }
    }
}





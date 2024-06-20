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

    public class SpecificationRequest
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





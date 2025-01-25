using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using System;

namespace SimaticArcorWebApi.Model.Custom
{
    public class NetSuiteConfigDto
    {
        public string BaseUrl { get; set; }
        public string ck { get; set; }
        public string cs { get; set; }
        public string tk { get; set; }
        public string ts { get; set; }
        public string realm { get; set; }
    }
}





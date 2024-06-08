using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom
{
    public class ProductionResponse
    {
        public OrderResponseObject Order { get; set; }
    }

    public class OrderResponseObject
    {
        public string Id { get; set; }
        public string href { get; set; }
    }
}

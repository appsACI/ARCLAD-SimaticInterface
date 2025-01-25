using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom.Quality
{
    public class ProductSpecificationRequestProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string UoM { get; set; }
        public float LowValue { get; set; }
        public float TargetValue { get; set; }
        public float HighValue { get; set; }
        public string Crit { get; set; }
        public int? Repetitions { get; set; }
        public int? Index { get; set; }
    }
}

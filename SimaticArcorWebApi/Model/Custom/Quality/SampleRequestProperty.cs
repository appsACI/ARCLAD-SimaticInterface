using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom.Quality
{
    public class SampleRequestProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string ActualValue { get; set; }
        public string UoM { get; set; }
    }
}

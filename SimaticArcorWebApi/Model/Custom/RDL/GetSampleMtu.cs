using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.RDL
{
    public class GetSampleMtu
    { 
        public string SampleNId { get; set; }
        public SampleProperties[] Properties { get; set; }
    }

    public class SampleProperties
    {
        public string NameProp { get; set; }
        public string ValueProp { get; set; }
    }
}

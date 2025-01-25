using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.Counters
{
    public class Counters
    {
        public string WoID { get; set; }
        public Params[] Parameters { get; set; }
        
    }

    public class Params {
        public string ParameterNId { get; set; }
        public string ParameterTargetValue { get; set; }
    }

}


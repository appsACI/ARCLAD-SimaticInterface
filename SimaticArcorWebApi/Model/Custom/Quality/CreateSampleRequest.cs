using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom.Quality
{
    public class CreateSampleRequest
    {
        public string Definition { get; set; }
        public string Lot { get; set; }
        public string IdCarga { get; set; }
        public string Datetime { get; set; }
        public string IdPlant { get; set; }
        public string SampleId { get; set; }
        public string IdProtocol { get; set; }
        public string Revision { get; set; }
        public SampleRequestProperty[] Properties { get; set; }
    }
}

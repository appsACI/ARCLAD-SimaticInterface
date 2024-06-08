using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom.Person
{
    public class CreatePersonRequest
    {
        public string plantID { get; set; }
        public string personnel { get; set; }
        public DateTime startTime { get; set; }
    }
}

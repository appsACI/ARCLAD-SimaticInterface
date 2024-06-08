using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic
{
    public class ApplicationLog
    {
        public string Id { get; set; }
        public string CorrelationId { get; set; }
        public int MessageId { get; set; }
    }

    public class ApplicationLogMessage
    {
        public string ShortMessage { get; set; }
        public string LongMessage { get; set; }
        public string Level { get; set; }
    }
}

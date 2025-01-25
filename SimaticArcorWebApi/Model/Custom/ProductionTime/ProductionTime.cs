using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.ProductionTime
{
    public class ProductionTime
    {
        public string Id { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public double Dias { get; set; }
        public double Horas { get; set; }
        public double Minutos { get; set; }
        public double Segundos { get; set; }
        public int EqIdentity { get; set; }
        public string EqName { get; set; }
        public string Estado { get; set; }
        public string Material { get; set; }
        public string Lote { get; set; }
        public string Orden { get; set; }
    }
}

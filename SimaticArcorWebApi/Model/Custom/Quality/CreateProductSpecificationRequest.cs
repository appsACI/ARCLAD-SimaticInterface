using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Custom.Quality
{
    public class CreateProductSpecificationRequest
    {
        public string Definition { get; set; }
        public string WC { get; set; }
        public string IdProtocol { get; set; }
        public string Revision { get; set; }
        public string IdPlant { get; set; }
        public string Sample { get; set; }
        public string Frequency { get; set; }
        public string UoMF { get; set; }
        public string descProtocol { get; set; }
        public List<ProductSpecificationRequestProperty> Properties { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SimaticWebApi.Model.Custom.RDL
{

    public class CreateRequirementModelCorte
    {
        public string NID { get; set; }
        public string LOTE { get; set; }
        public string PLANTA { get; set; }
        public string ID_ARCLAD { get; set; }
        public string FECHA_ANALISIS { get; set; }
        public string IF_WORKORDER { get; set; }
        public string IF_REFERENCIA { get; set; }
        public string IF_ANCHO_SIL { get; set; }
        public string IF_LONGITUD_SIL { get; set; }
        public string IF_DEFECTO_REPORTADO { get; set; }
        public string IF_ROLLO_ESTIBA { get; set; }
        public string IF_CLIENTE { get; set; }
        public string IF_TURNODESP { get; set; }
        public string IF_MAQUINASDESP { get; set; }
        public string IF_LONG_ESTAND { get; set; }
    }
}

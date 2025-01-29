using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.TransactionalLog
{
    public class TransactionalLogModel
    {
        public string Planta { get; set; } = "";                     
        public string ProgramaFuente { get; set; } = "";
        public string ProgramaDestino { get; set; } = "";
        public string Medio { get; set; } = "SWAGGER";
        public string Proceso { get; set; } = "";
        public string StatusCode { get; set; } = "";
        public string Comando { get; set; } = "";
        public string ReasonStatus { get; set; } = "";
        public string Maquina { get; set; } = "";
        public string Usuario { get; set; } = "SITINTEGRATION";
        public string Tipo { get; set; } = "";
        public string WorkOrders { get; set; } = "";
        public string Lotes { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Trim { get; set; } = "";
        public string NTrim { get; set; } = "";
        public string Cortes { get; set; } = "";
        public string URL { get; set; } = "";
        public string Payload { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string Succeced { get; set; } = "";
        public string ErrorCode { get; set; } = "";

    }
}

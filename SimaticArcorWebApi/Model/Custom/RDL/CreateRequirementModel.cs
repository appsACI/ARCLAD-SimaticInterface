using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.RDL
{
    public class CreateRequirementModel
    {
        public Requirement REQUIREMENT { get; set; }
    }

    public class Requirement
    {
        public string SHORT_DESC { get; set; }
        public string LOTE { get; set; }
        public string LOTE_SILICONA_3 { get; set; }
        public string ID_ADHESIVO { get; set; }
        public string PROVEEDOR_MP_RESPALDO { get; set; }
        public string AR_ANCHO { get; set; }
        public string LADOAPLICACION_SILICONA { get; set; }
        public string IF_LOTE_FOTOPOLIMERO { get; set; }
        public string WORK_ORDER { get; set; }
        public string PLANTA { get; set; }
        public string ID_ARCLAD { get; set; }
        public string FECHA_DE_ANALISIS { get; set; }
        public string REFERENCIA { get; set; }
        public string LONGITUD_M { get; set; }
        public string REFERENCIA_SILICONA { get; set; }
        public string ID_SILICONA { get; set; }
        public string LOTE_SILICONA_1 { get; set; }
        public string LOTE_SILICONA_2 { get; set; }
        public string REFERENCIA_CARA_IMPRESION { get; set; }
        public string ID_CARA_IMPRESION { get; set; }
        public string PROVEEDOR_CARA_IMPRESION { get; set; }
        public string LOTE_CARA_IMPRESION_1 { get; set; }
        public string LOTE_CARA_IMPRESION_2 { get; set; }
        public string LOTE_CARA_IMPRESION_3 { get; set; }
        public string LOTE_ADHESIVO_1 { get; set; }
        public string LOTE_ADHESIVO_2 { get; set; }
        public string LOTE_ADHESIVO_3 { get; set; }
        public string REFERENCIA_ADHESIVO { get; set; }
        public string PROVEEDOR_ADHESIVO { get; set; }
        public string LADO_APLICACION_ADHESIVO { get; set; }
        public string REFERENCIA_PRIMER { get; set; }
        public string ID_PRIMER { get; set; }
        public string PROVEEDOR_PRIMER { get; set; }
        public string LOTE_PRIMER_1 { get; set; }
        public string LOTE_PRIMER_2 { get; set; }
        public string MAQUINA { get; set; }
        public string REFERENCIA_MP_RESPALDO { get; set; }
        public string ID_MP_RESPALDO { get; set; }
        public string LOTE_RESPALDO_1 { get; set; }
        public string LOTE_RESPALDO_2 { get; set; }
        public string LOTE_RESPALDO_3 { get; set; }
        public string SOLICITANTE { get; set; }
        public string AR_CATEGORIA { get; set; }
        public string AR_GRUPO { get; set; }
        public string FECHA_INGRESO { get; set; }
        public string ORDEN_COMPRA { get; set; }
        public string PROVEEDOR { get; set; }
        public string REFERENCIA_PROVEEDOR { get; set; }
        public string CANTIDAD_RECIBIDA { get; set; }
        public string UNIDAD { get; set; }
        public string CADUCIDAD { get; set; }
        public string AR_CLASE { get; set; }
        public string IF_IMPRESO { get; set; }
        public string IF_ARTE { get; set; }
        public string IF_DATOSCARAIMPR { get; set; }
        public string IF_DATOSADHESIVO { get; set; }
        public string IF_DATOS_PRIMER { get; set; }
        public string IF_TURNODESP { get; set; }
        public string IF_DATOS_SIL { get; set; }
        public string ID_LAM_MASA { get; set; }
        public string IF_IDFOTOPOLIMERO { get; set; }
        public string IF_OPERARIO1 { get; set; }
        public string IF_OPERARIO2 { get; set; }
        public string IF_LOTE_LAM_MASA { get; set; }
        public string ANCHO_M { get; set; }
    }

    public class Token
    {
        public string Type { get; set; }
        public string Val { get; set; }
        public string NId { get; set; }
        public bool IsProtected { get; set; }
        public bool IsMultiValue { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string Category { get; set; }
        public bool IsSystemDefined { get; set; }
    }
}

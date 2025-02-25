using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.PrintLabel
{
    public class PrintModel
    {
        public string PrintOperation { get; set; }
        public string LabelType { get; set; }
        public string LabelTemplate { get; set; }
        public string LabelPrinter { get; set; }
        public string NoOfCopies { get; set; }
        public string Equipment { get; set; }
        public MezclasPrintModel LabelTagsMezclas { get; set; }
        public RecubrimientoModel LabelTagsRecubrimiento { get; set; }
        public RolloPrintModel LabelTagsRollos { get; set; }
        public ARSEGPrintModel LabelTagsArseg { get; set; }
        public OtherTagsModel LabelTagsOther { get; set; }


    }

    public class PrinterModel
    {
        public string LabelType { get; set; }
        public string LabelTemplate { get; set; }
        public string LabelPrinter { get; set; }
        public TagModel[] LabelTags { get; set; }
        public string NoOfCopies { get; set; }
    }

    public class TagModel
    {
        public string TagName { get; set; }
        public string TagValue { get; set; }
    }

    public class MezclasPrintModel
    {
        public string TagOperario { get; set; }
        public string TagOrdenCompra { get; set; }
        public string TagProveedor { get; set; }
        public string TagReferencia { get; set; }
        public string TagUom { get; set; }
        public decimal TagCantidad { get; set; }
        public string TagDatetime { get; set; }
        public string TagDescripcion { get; set; }
        public string Tagfecha_vcto { get; set; }
        public string TagLote { get; set; }
        public string TagLote_proveedor { get; set; }
        public string TagMaterial { get; set; }
        public string TagObservaciones { get; set; }
        public string TagFecha_fab { get; set; }
    }

    public class RecubrimientoModel
    {
        #region Datos Basicos
        public string TagPphora { get; set; }
        public string TagPpdate { get; set; }
        public string TagPpmaquina { get; set; }
        public string TagPpoperario { get; set; }
        public string TagPporden { get; set; }
        public string TagPplote { get; set; }
        #endregion

        public string TagPpid { get; set; }
        public string TagPplamfint { get; set; }

        public string TagPpmetroscuadrados { get; set; }
        public string TagPpmts2defec { get; set; }
        public string TagPpmtslindefc { get; set; }
        public string TagPpmtspra { get; set; }
        public string TagPpmts2pra { get; set; }
        public string TagPpobservaciones { get; set; }
        public string TagPpreferencia { get; set; }

        #region Campos Formato
        public string TagPpcodigo { get; set; } //este debe ir vacio
        public string TagPpfechaformato { get; set; }//este debe ir vacio
        public string TagPpresponsable { get; set; }//este debe ir vacio
        #endregion

        #region Campos de Defectos

        #region Campo metraje Final
        public string TagPpmf1 { get; set; }
        public string TagPpmf2 { get; set; }
        public string TagPpmf3 { get; set; }
        public string TagPpmf4 { get; set; }
        public string TagPpmf5 { get; set; }
        public string TagPpmf6 { get; set; }
        public string TagPpmf7 { get; set; }
        #endregion

        #region Campo metraje inicial
        public string TagPpmi1 { get; set; }
        public string TagPpmi2 { get; set; }
        public string TagPpmi3 { get; set; }
        public string TagPpmi4 { get; set; }
        public string TagPpmi5 { get; set; }
        public string TagPpmi6 { get; set; }
        public string TagPpmi7 { get; set; }
        #endregion

        #region Campo 'C'
        public string TagPpc1 { get; set; }
        public string TagPpc2 { get; set; }
        public string TagPpc3 { get; set; }
        public string TagPpc4 { get; set; }
        public string TagPpc5 { get; set; }
        public string TagPpc6 { get; set; }
        public string TagPpc7 { get; set; }
        #endregion

        #region Campo 'D'
        public string TagPpd1 { get; set; }
        public string TagPpd2 { get; set; }
        public string TagPpd3 { get; set; }
        public string TagPpd4 { get; set; }
        public string TagPpd5 { get; set; }
        public string TagPpd6 { get; set; }
        public string TagPpd7 { get; set; }
        #endregion

        #region Campo 'G'
        public string TagPpg1 { get; set; }
        public string TagPpg2 { get; set; }
        public string TagPpg3 { get; set; }
        public string TagPpg4 { get; set; }
        public string TagPpg5 { get; set; }
        public string TagPpg6 { get; set; }
        public string TagPpg7 { get; set; }
        #endregion

        #region Campo Codigo del defecto
        public string TagPpcod1 { get; set; }
        public string TagPpcod2 { get; set; }
        public string TagPpcod3 { get; set; }
        public string TagPpcod4 { get; set; }
        public string TagPpcod5 { get; set; }
        public string TagPpcod6 { get; set; }
        public string TagPpcod7 { get; set; }
        #endregion

        #region Campo Descripcion defecto
        public string TagPpdesdf1 { get; set; }
        public string TagPpdesdf2 { get; set; }
        public string TagPpdesdf3 { get; set; }
        public string TagPpdesdf4 { get; set; }
        public string TagPpdesdf5 { get; set; }
        public string TagPpdesdf6 { get; set; }
        public string TagPpdesdf7 { get; set; }
        #endregion

        #region Campo metros de defecto
        public string TagPpmd1 { get; set; }
        public string TagPpmd2 { get; set; }
        public string TagPpmd3 { get; set; }
        public string TagPpmd4 { get; set; }
        public string TagPpmd5 { get; set; }
        public string TagPpmd6 { get; set; }
        public string TagPpmd7 { get; set; }

        #endregion

        #endregion

    }

    public class RolloPrintModel
    {
        public string TagPtacnho { get; set; }
        public string TagPtanchodecimales { get; set; }
        public string TagPtcantidad { get; set; }
        public string TagPtdate { get; set; }
        public string TagPtdatetime { get; set; }
        public string TagPtdescripcion { get; set; }
        public string TagPtdimensiones { get; set; }
        public string TagPtidmaterial { get; set; }
        public string TagPtlargo { get; set; }
        public string TagPtlote { get; set; }
        public string TagPtmadeincolombia { get; set; }
        public string TagPtoperario { get; set; }
        public string TagPtordendeproduccion { get; set; }
        public string TagPtpesoaproximado { get; set; }
        public string TagPtqr { get; set; }
        public string TagPtreferencia { get; set; }
        public string TagPtresumenrollo { get; set; }
        public string TagPtrollo { get; set; }
        public string TagPtuoman { get; set; }
        public string TagPtuomlg { get; set; }
        public string TagPtempate { get; set; }
        public string TagPtrevision { get; set; }

    }

    public class ARSEGPrintModel
    {
        public string TagArcantidad { get; set; }
        public string TagArcantidadm2 { get; set; }
        public string TagArcliente { get; set; }
        public string TagArcodcliente { get; set; }
        public string TagArdate { get; set; }
        public string TagArdatetime { get; set; }
        public string TagArdescripcion { get; set; }
        public string TagArdescripcion1 { get; set; }
        public string TagArdimensionalto { get; set; }
        public string TagArdimensionancho { get; set; }
        public string TagAridmaterial { get; set; }
        public string TagArlote { get; set; }
        public string TagArnrodecaja { get; set; }
        public string TagArnrollo { get; set; }
        public string TagArobservaciones { get; set; }
        public string TagAroperario { get; set; }
        public string TagArordencompra { get; set; }
        public string TagArordenproduccion { get; set; }
        public string TagArordenventa { get; set; }
        public string TagArpesoaproximado { get; set; }
        public string TagArqr { get; set; }
        public string TagArreferencia { get; set; }
        public string TagArtelefono { get; set; }
        public string TagArunidadesxcaja { get; set; }
        public string TagARrevision { get; set; }

    }

    public class OtherTagsModel
    {
        public string TagPtcancho { get; set; }
        public string TagPtccantidad { get; set; }
        public string TagPtccodigo { get; set; }
        public string TagPtcconsecutivoestiba { get; set; }
        public string TagPtcdate { get; set; }
        public string TagPtcdatetime { get; set; }
        public string TagPtcdescripcion { get; set; }
        public string TagPtcidmaterial { get; set; }
        public string TagPtcidrollo { get; set; }
        public string TagPtclargo { get; set; }
        public string TagPtclongitudml { get; set; }
        public string TagPtclote { get; set; }
        public string TagPtcobservaciones { get; set; }
        public string TagPtcoperario { get; set; }
        public string TagPtcordencompra { get; set; }
        public string TagPtcordenproduccion { get; set; }
        public string TagPtcpesoaproximado { get; set; }
        public string TagPtcqr { get; set; }
        public string TagPtcreferencia { get; set; }
        public string TagPtcresumenrollo { get; set; }
        public string TagPtcrollo { get; set; }
        public string TagPtctime { get; set; }
        public string TagPtcuom { get; set; }
        public string TagPtcuomancho { get; set; }
        public string TagPtcuomlg { get; set; }
        public string TagPtconsecutivo { get; set; }
        public string TagPtestiba { get; set; }
        public string TagPtconsecutivoestiba { get; set; }
    }

    public class Reimpresion
    {
        public string HistoryId { get; set; }
        public string NoOfCopies { get; set; }
        public string LabelPrinter { get; set; }
        public string Reason { get; set; }

    }

    public class ReimpresionFilters
    {
        public string DateStart { get; set; }
        public string DateEnd { get; set; }
        //public string EquipmentId { get; set; }
        public string LoteId { get; set; }
    }

    public class LabelHistory
    {
        public DateTime Fecha { get; set; }
        public string Lote { get; set; }
        public string HistoryId { get; set; }
        public string Reason { get; set; }
        public bool RePrint { get; set; }

    }

    public class LabelPrintHistory
    {
        public string Id { get; set; }
        public string AId { get; set; }
        public bool IsFrozen { get; set; }
        public int ConcurrencyVersion { get; set; }
        public int IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string EntityType { get; set; }
        public string OptimisticVersion { get; set; }
        public string ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public bool IsReprint { get; set; }
        public string LabelTypeDescription { get; set; }
        public string LabelTemplateNId { get; set; }
        public string LabelTemplateName { get; set; }
        public string LabelTemplateDescription { get; set; }
        public string LabelTemplateSourceFileName { get; set; }
        public string LabelTemplateFormattingCulture { get; set; }
        public string IsReprintOf { get; set; }
        public string LabelTypeNId { get; set; }
        public string LabelTypeName { get; set; }
        public string LabelTemplateTagBeginDelimiter { get; set; }
        public string LabelTemplateTagEndDelimiter { get; set; }
        public string LabelPrinterDescription { get; set; }
        public string LabelPrinterNId { get; set; }
        public string LabelPrinterName { get; set; }
        public string LabelPrinterLabelingSystemInformation { get; set; }
        public string State { get; set; }
        public string StateDetails { get; set; }
        public string InitiatedBy { get; set; }
        public string Reason { get; set; }
        public string LabelTemplateSourceFile_Id_Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string LabelTemplateId { get; set; }
        public string LabelTemplateEncoding { get; set; }
        public string LabelTypeId { get; set; }
        public string LabelPrinterId { get; set; }
        public string LabelPrinterLabelingSystem { get; set; }
        public int NoOfCopies { get; set; }
    }

    public class LabelPrintHistoryTag
    {
        public string Id { get; set; }
        public string AId { get; set; }
        public bool IsFrozen { get; set; }
        public int ConcurrencyVersion { get; set; }
        public int IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public string EntityType { get; set; }
        public string OptimisticVersion { get; set; }
        public string ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public string TagName { get; set; }
        public string TagValue { get; set; }
        public string LabelPrintHistory_Id { get; set; }
    }
}

#region Json Print Command
#region json Mezclas
//{
//"command": { 
//            "LabelType": "ProductoenProcesoMezclasLbl",
//            "LabelTemplate": "Materiaprima",
//            "LabelPrinter": "PRINT1",
//            "LabelTags": [
//                {
//                    "TagName": "operario",
//                    "TagValue": "FabiTEst"
//                },
//                {
//                    "TagName": "orden_compra",
//                    "TagValue": "12345"
//                },
//                {
//                    "TagName": "proveedor",
//                    "TagValue": "proveedor de prueba"
//                },
//                {
//                    "TagName": "referencia",
//                    "TagValue": "ADHESIVO P1 ALTA VISCOSIDAD R1 Y R2"
//                },
//                {
//                    "TagName": "uom",
//                    "TagValue": "Kg"
//                },
//                {
//                    "TagName": "cantidad",
//                    "TagValue": "1000"
//                },
//                {
//                    "TagName": "datetime",
//                    "TagValue": "07/02/2024 00:00:00"
//                },
//                {
//                    "TagName": "descripcion",
//                    "TagValue": "pruebaComando"
//                },
//                {
//                    "TagName": "fecha_vcto",
//                    "TagValue": "07/02/2026"
//                },
//                {
//                    "TagName": "lote",
//                    "TagValue": "2402-0000501M09"
//                },
//                {
//                    "TagName": "lote_proveedor",
//                    "TagValue": "lote proveedor ABC"
//                },
//                {
//                    "TagName": "material",
//                    "TagValue": "mat prueba"
//                },
//                {
//                    "TagName": "observaciones",
//                    "TagValue": "Me lleva el chanfle"
//                }
//            ],
//            "NoOfCopies": "1"
//        }
//}

#endregion

#region json recubrimientos

//{
//   "command":{
//      "LabelType":"ProductoenProcesoLbl",
//      "LabelTemplate":"productoenprocesozpl",
//      "LabelPrinter":"PRINT1",
//      "LabelTags":[
//         {
//            "TagName":"ppc1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppc7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcantidadh",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcod7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppcodigo",
//            "TagValue":"50"
//         },
//         {
//            "TagName":"ppd1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppd7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdate",
//            "TagValue":"12/02/2024"
//         },
//         {
//            "TagName":"ppdesdf1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppdesdf7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppfechaformato",
//            "TagValue":"17/02/2024"
//         },
//         {
//            "TagName":"ppg1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppg7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"pphora",
//            "TagValue":"10:30"
//         },
//         {
//            "TagName":"ppid",
//            "TagValue":"25L00.001.0710-01"
//         },
//         {
//            "TagName":"pplamfint",
//            "TagValue":"1200"
//         },
//         {
//            "TagName":"pplote",
//            "TagValue":"2402-00005A"
//         },
//         {
//            "TagName":"ppmaquina",
//            "TagValue":"R09"
//         },
//         {
//            "TagName":"ppmd1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmd7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmedida",
//            "TagValue":"1000"
//         },
//         {
//            "TagName":"ppmetroscuadrados",
//            "TagValue":"1000"
//         },
//         {
//            "TagName":"ppmf1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmf7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi1",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi2",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi3",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi4",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi5",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi6",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmi7",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmne",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppmts2defec",
//            "TagValue":"2220"
//         },
//         {
//            "TagName":"ppmtspra",
//            "TagValue":"2000"
//         },
//         {
//            "TagName":"ppobservaciones",
//            "TagValue":"0"
//         },
//         {
//            "TagName":"ppoperario",
//            "TagValue":"Op2"
//         },
//         {
//            "TagName":"pporden",
//            "TagValue":"WOC000162"
//         },
//         {
//            "TagName":"ppreferencia",
//            "TagValue":"LU60-BH-G58S-0710"
//         },
//         {
//            "TagName":"ppresponsable",
//            "TagValue":"Respon1"
//         }
//      ],
//      "NoOfCopies":"1"
//   }
//}

#endregion

#endregion
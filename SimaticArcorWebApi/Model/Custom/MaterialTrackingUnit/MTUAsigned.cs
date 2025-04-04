﻿using SimaticArcorWebApi.Management;
using System;

namespace SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit
{
    public class MTUAsignedArray
    {
        public MTUAsigned[] MaterialList { get; set; }
    }

    public class MTUAsigned
    {
        public string Id { get; set; }
        public string MaterialNId { get; set; }
        public string Mtu { get; set; }
        public decimal MtuQuantity { get; set; }
        public string WorkOrderNId { get; set; }
        public string MtuUoM { get; set; }
    }

    public class MTUUnassign
    {
        public DeleteMaterialRequirementMTU[] Ids { get; set; }
    }

    public class MTUAssignedUnassign
    {
        public string WorkOrderNid { get; set; }
        public string OperationId { get; set; }
        public string[] MTUNid { get; set; }
        public string Equipment { get; set; }
        public string Printer { get; set; }
        public string Operario { get; set; }
    }

    public class DeleteMaterialRequirementMTU
    {
        public string IdArray { get; set; }
    }

    public class MTUId
    {
        public Guid Id { get; set; }
    }

    public class MTUPropiedadesDefectos
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Cantidad { get; set; }
        public string Descripcion { get; set; }
        public string Metraje_Inicial { get; set; }
        public string Metraje_Final { get; set; }
        public string C { get; set; }
        public string G { get; set; }
        public string O { get; set; }
        public string TotalM2 { get;  set; }
        public string TotalM { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using SimaticArcorWebApi.Model.Simatic.MTU;

namespace SimaticArcorWebApi.Model.Custom.BulkReception
{
    public class BulkReception
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
        public object ConcurrencyToken { get; set; }
        public bool IsLocked { get; set; }
        public bool ToBeCleaned { get; set; }
        public string OC { get; set; }
        public string Material { get; set; }
        public string Lote { get; set; }
        public string NumeroLinea { get; set; }
        public string Patente { get; set; }
        public string Planta { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string IdCarga { get; set; }
        public DateTime Fecha { get; set; }
        public MTUQuantity Quantity { get; set; }
    }
}

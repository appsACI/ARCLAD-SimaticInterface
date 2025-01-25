using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic.BOM
{
  public class BillOfMaterialsItem
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
    public string NId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string MaterialNId { get; set; }
    public object MaterialRevision { get; set; }
    public object OperationNId { get; set; }
    public object OperationRevision { get; set; }
    public int Sequence { get; set; }
    public string UseBillOfMaterials { get; set; }
    public object MaterialGroup_Id { get; set; }
    public string BillOfMaterials_Id { get; set; }
    public BillOfMaterialsItemAlternative Alternative { get; set; }
    public BillOfMaterialsItemQuantity Quantity { get; set; }
    public object MaterialGroup { get; set; }
  }
}

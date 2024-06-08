using System;

namespace SimaticArcorWebApi.Model.Simatic.MaterialLot
{
  public class MaterialLot
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
    public string Name { get; set; }
    public string Description { get; set; }
    public string NId { get; set; }
    public string MaterialNId { get; set; }
    public object MaterialRevision { get; set; }
    public object MaterialUId { get; set; }
    public object TemplateNId { get; set; }
    public MaterialLotStatus Status { get; set; }
    public MaterialLotQuantity Quantity { get; set; }
  }
}
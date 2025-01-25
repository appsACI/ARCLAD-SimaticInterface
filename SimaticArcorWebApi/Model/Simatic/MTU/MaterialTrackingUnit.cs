using System;

namespace SimaticArcorWebApi.Model.Simatic.MTU
{
  public class MaterialTrackingUnit
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
    public object Description { get; set; }
    public string MaterialNId { get; set; }
    public string MaterialRevision { get; set; }
    public string MaterialUId { get; set; }
    public string EquipmentNId { get; set; }
    public object TemplateNId { get; set; }
    public string Code { get; set; }
    public string CodeType { get; set; }
    public string MaterialLot_Id { get; set; }
    public object MaterialTrackingUnitAggregate_Id { get; set; }
    public MTUStatus Status { get; set; }
    public MTUQuantity Quantity { get; set; }
  }
}
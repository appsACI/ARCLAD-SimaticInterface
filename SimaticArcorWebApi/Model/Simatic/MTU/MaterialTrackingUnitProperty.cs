using System;

namespace SimaticArcorWebApi.Model.Simatic.MTU
{
  public class MaterialTrackingUnitProperty
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
    public string NId { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyUoMNId { get; set; }
    public string PropertyType { get; set; }
    public string MaterialTrackingUnit_Id { get; set; }
  }

}
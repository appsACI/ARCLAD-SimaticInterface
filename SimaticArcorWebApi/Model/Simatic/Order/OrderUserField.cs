using System;

namespace SimaticArcorWebApi.Model.Simatic.Order
{
  public class OrderUserField
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
    public string UserFieldType { get; set; }
    public string UserFieldValue { get; set; }
    public string Order_Id { get; set; }
    public object[] SegregationTags { get; set; }
  }

  /// <summary>
  /// Interoperability UserField structure
  /// </summary>
  public class IOOrderUserField
  {
    public string NId { get; set; }
    public string UserFieldType { get; set; }
    public string UserFieldValue { get; set; }

    }
}
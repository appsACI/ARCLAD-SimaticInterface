using System;

namespace SimaticArcorWebApi.Model.Simatic.WorkOrder
{
  public class WorkOrderExtended
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
    public string WorkOrder_Id { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public object ActualStartTime { get; set; }
    public object ActualEndTime { get; set; }
    public object SourceWorkOrderNId { get; set; }
    public string WorkMasterNId { get; set; }
    public string WorkMasterRevision { get; set; }
    public string OrderNId { get; set; }
    public string Type { get; set; }
    public bool IsFavourite { get; set; }
    public WorkOrderExtendedActualQuantity ActualQuantity { get; set; }
  }
}
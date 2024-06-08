using System;

namespace SimaticArcorWebApi.Model.Simatic.RoadMap
{
  public class RoadMapOperation
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
    public float Cost { get; set; }
    public object Crew { get; set; }
    public object Efficiency { get; set; }
    public float KgHs { get; set; }
    public float KgHsMach { get; set; }
    public string Level { get; set; }
    public float MachineHs { get; set; }
    public float ManHs { get; set; }
    public string Name { get; set; }
    public DateTime OpFrom { get; set; }
    public DateTime OpTo { get; set; }
    public float Theoretical { get; set; }
    public string RoadMap_Id { get; set; }
    public RoadMap RoadMap { get; set; }
  }
  
}
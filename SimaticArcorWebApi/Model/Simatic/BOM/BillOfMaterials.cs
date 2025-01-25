using System;

namespace SimaticArcorWebApi.Model.Simatic.BOM
{
  public class BillOfMaterials
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
    public string Revision { get; set; }
    public object SourceRevision { get; set; }
    public bool IsCurrent { get; set; }
    public string NId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string TemplateNId { get; set; }
    public ReferenceQuantity ReferenceQuantity { get; set; }
  }

  public class MasterDataTemplate
  {
    public Guid Id { get; set; }
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
    public bool IsDefaut { get; set; }
  }
}
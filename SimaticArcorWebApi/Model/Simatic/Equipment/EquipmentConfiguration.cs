using System;

namespace SimaticArcorWebApi.Model.Simatic.Equipment
{
  public class EquipmentConfiguration
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
    public string NId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public object LocationNId { get; set; }
    public string EquipmentTypeNId { get; set; }
    public string LevelNId { get; set; }
    public EquipmentConfigurationProperty[] EquipmentProperties { get; set; }
  }

  public class Equipment
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
    public string NId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public object WorkCalendarNId { get; set; }
    public string EquipmentConfigurationId { get; set; }
    public string LevelNId { get; set; }
    public Status Status { get; set; }
    public EquipmentProperty[] EquipmentProperties { get; set; }
  }

  public class EquipmentProperty
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
    public string NId { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyType { get; set; }
    public bool Operational { get; set; }
    public Guid Equipment_Id { get; set; }
  }

  public class EquipmentConfigurationProperty
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
    public string NId { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyType { get; set; }
    public bool Operational { get; set; }
    public Guid EquipmentConfiguration_Id { get; set; }
  }

  public class EquipmentConfigurationPropertyType
  {
    public string NId { get; set; }
    public string PropertyValue { get; set; }
    public string PropertyType { get; set; }
    public bool Operational { get; set; }
  }

  public class EquipmentPropertyType
  {
    public Guid EquipmentPropertyId { get; set; }
    public string EquipmentPropertyValue { get; set; }
  }

  public class Status
  {
    public string StateMachineNId { get; set; }
    public string StatusNId { get; set; }
  }

}
using System;

namespace SimaticArcorWebApi.Model.Custom
{
  public class EquipmentPQRNotification
  {
    public string EquipmentId { get; set; }
    public string SupportCaseId { get; set; }
  }

  public class EquipmentPreventiveMaintenaceNotification
  {
    public string OrderId { get; set; }
    public string EquipmentNId { get; set; }
    public string Tipo { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; }
    public string Comments { get; set; }
  }
}
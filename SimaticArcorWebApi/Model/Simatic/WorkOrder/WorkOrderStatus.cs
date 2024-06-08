namespace SimaticArcorWebApi.Model.Simatic.WorkOrder
{
  public class WorkOrderStatus
  {
    public string StateMachineNId { get; set; }
    public string StatusNId { get; set; }
  }

  public class WorkOrderOperationStatus
  {
    public string StateMachineNId { get; set; }
    public string StatusNId { get; set; }
  }
}
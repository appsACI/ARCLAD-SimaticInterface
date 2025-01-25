using System;

namespace SimaticArcorWebApi.Modules.DCMovement
{
  public class DCMovementRequest
  {
    public string Id { get; set; }
    public string MaterialDefinitionID { get; set; }
    public string Status { get; set; }
    public DCMovementRequestLocation Location { get; set; }
    public DCMovementRequestQuantity Quantity { get; set; }
    public DCMovementRequestMaterialLotProperty[] MaterialLotProperty { get; set; }
  }
}
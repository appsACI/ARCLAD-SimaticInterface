namespace SimaticArcorWebApi.Model.Custom.G8Response
{
  public class G8ResponseRequest
  {
    public string Id { get; set; }
    public string MaterialDefinitionId { get; set; }
    public string Status { get; set; }
    public G8ResponseRequestMaterialLotProperty[] MaterialLotProperty { get; set; }
    public G8ResponseRequestLocation Location { get; set; }
    public G8ResponseRequestQuantity Quantity { get; set; }
  }
}

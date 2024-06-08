namespace SimaticArcorWebApi.Model.Custom.PalletReception
{
  public class CreatePalletReceptionRequest
  {
    public string OT { get; set; }

    public string Material { get; set; }

    public PalletRequestQuantity Quantity { get; set; }

    public string Lot { get; set; }

    public PalletRequestProperty[] Properties { get; set; }
  }
}
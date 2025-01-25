using System;

namespace SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit
{
  public class TruckReceiveRequest
  {
    public string Material { get; set; }
    public string IdCarga { get; set; }
    public string OC { get; set; }
    public string Plate { get; set; }
    public string Lot { get; set; }
    public string NroLinea { get; set; }
    public string Plant { get; set; }
    public string Estado { get; set; }
    public float Qty { get; set; }
    public float Kilogramos { get; set; }
    public string UoM { get; set; }
    public DateTime Datetime { get; set; }

  }
}
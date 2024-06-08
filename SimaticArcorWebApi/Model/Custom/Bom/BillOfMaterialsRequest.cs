using System;

namespace SimaticArcorWebApi.Model.Custom.Bom
{
  public class BillOfMaterialsRequest
  {
    public string Id { get; set; }
    public string MaterialId { get; set; }
    public string Plant { get; set; }
    public double? QuantityValue { get; set; }
    public string UoMNId { get; set; }
    public string List { get; set; }
    public BillOfMaterialsRequestItem[] Items { get; set; }
    public BillOfMaterialsRequestProperty[] Properties { get; set; }
  }
}
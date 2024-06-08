using System;

namespace SimaticArcorWebApi.Model.Custom.Bom
{
  public class BillOfMaterialsRequestItem
  {
    public string MaterialId { get; set; }
    public string Description { get; set; }
    public double? QuantityValue { get; set; }
    public string UoMNId { get; set; }
    public int Sequence { get; set; }
    public double? Scrap { get; set; }
    //public DateTime From { get; set; }
    //public DateTime To { get; set; }
  }

  public class BillOfMaterialsRequestProperty
  {
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Uom { get; set; }
  }
}
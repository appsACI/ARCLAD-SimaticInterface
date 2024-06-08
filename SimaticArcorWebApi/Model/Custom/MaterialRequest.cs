using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Modules;

namespace SimaticArcorWebApi.Model.Custom
{
  public class MaterialRequest
  {
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string TemplateNId { get; set; }

    public string Uom { get; set; }

    public string Status { get; set; }

    public string InitialLotStatus { get; set; }

    public MaterialRequestProperty[] Properties { get; set; }
  }
}
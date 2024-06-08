namespace SimaticArcorWebApi.Management
{
  public interface IUOMService
  {
    string GetSimaticUOM(string uom);

    string GetNetSuiteUOM(string uom);
  }
}
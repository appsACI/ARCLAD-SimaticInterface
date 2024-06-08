using System.Collections.Generic;

namespace SimaticArcorWebApi.Model.Simatic
{
  public class UomConfiguration
  {
    public Dictionary<string, string> SimaticToNetSuite { get; set; }
    public Dictionary<string, string> NetSuiteToSimatic { get; set; }
  }
}
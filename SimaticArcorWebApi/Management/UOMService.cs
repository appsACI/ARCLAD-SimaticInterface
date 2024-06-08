using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Simatic;

namespace SimaticArcorWebApi.Management
{
  public class UOMService : IUOMService
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<UOMService> logger;

    public UomConfiguration Config { get; set; }

    public UOMService(IConfiguration config)
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<UOMService>();

      Config = new UomConfiguration();
      config.GetSection("UomConfiguration").Bind(Config);
    }

    public string GetSimaticUOM(string uom)
    {
      return Config.NetSuiteToSimatic.ContainsKey(uom.ToLower()) ? Config.NetSuiteToSimatic[uom.ToLower()] : uom;
    }

    public string GetNetSuiteUOM(string uom)
    {
      return Config.SimaticToNetSuite.ContainsKey(uom.ToLower()) ? Config.SimaticToNetSuite[uom.ToLower()] : uom;
    }
  }
}

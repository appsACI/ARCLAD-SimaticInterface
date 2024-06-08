using Nancy;
using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Nancy.ModelBinding;
using Serilog;
using SimaticArcorWebApi.Management;

namespace SimaticArcorWebApi.Modules
{
  public class HealthModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<HealthModule> logger;

    public HealthModule(IMaterialService materialService) : base("api/health")
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<HealthModule>();

      Get("/", GetHealth);
    }

    private async Task<dynamic> GetHealth(object arg1, CancellationToken ct)
    {
      try
      {
        return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(new
        {
          online = true,
          message = "the api works fine."
        });
      }
      catch (Exception e)
      {
        logger.LogError("Error trying to get data", e);

        return Negotiate
          .WithStatusCode(HttpStatusCode.InternalServerError);
      }
    }


  }



}

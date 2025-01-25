using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Nancy;
using Nancy.ModelBinding;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;

namespace SimaticArcorWebApi.Modules
{
  public class HomeModule : NancyModule
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private static readonly ILog Log = LogManager.GetLogger<HomeModule>();

    /// <summary>
    /// Gets or sets the material service component
    /// </summary>
    public IMaterialService MaterialService { get; set; }

    public HomeModule(IMaterialService materialService)
    {
      this.MaterialService = materialService;

      base.Get("/docs", x =>
            {
              var site = Request.Url.SiteBase;
              return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

      Post("/CustomOp", CustomOp, name: "PostCustomOperation");

    }

    private async Task<dynamic> CustomOp(dynamic parameters, CancellationToken ct)
    {
      try
      {
        var ids = this.Bind<string[]>();

        foreach (var id in ids)
        {
          try
          {
            //await SimaticService.SetBillOfMaterialsRevisionCurrentAsync(id, ct);
            //await SimaticService.DeleteMaterialAsync(id, ct); 
          }
          catch (Exception e)
          {
            Log.ErrorFormat($"Error trying to create material. Id: [{id}]");
          }
        }

        return Negotiate.WithStatusCode(HttpStatusCode.OK).WithHeader("Location", "http://localhost:5577/api/test/123");
      }
      catch (SimaticApiException e)
      {
        return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
      }
      catch (Exception e)
      {
        Log.Error(e);
        return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
      }
    }

  }
}

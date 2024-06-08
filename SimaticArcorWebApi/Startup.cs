using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SimaticArcorWebApi.Jobs;
using SimaticArcorWebApi.Management;

[assembly: HostingStartup(typeof(SimaticArcorWebApi.Startup))]

namespace SimaticArcorWebApi
{
  public class Startup : IHostingStartup
  {
    public void Configure(IWebHostBuilder builder)
    {
      builder.ConfigureKestrel(o =>
      {
        o.AllowSynchronousIO = true;
      });

      builder.ConfigureAppConfiguration((ctx, c) =>
      {

      });

      builder.ConfigureServices((ctx, c) =>
      {
        c.AddSingleton(ctx.Configuration);
        c.AddHostedService<JobMonitor>();
        c.AddSingleton<IMaterialService, MaterialService>();
        c.AddSingleton<IBOMService, BOMService>();
        c.AddSingleton<IMTUService, MTUService>();
        c.AddSingleton<IUOMService, UOMService>();
        c.AddSingleton<IWorkOrderCompletionService, WorkOrderCompletionService>();
        c.AddSingleton<ISimaticService, SimaticService>();
        c.AddSingleton<ISimaticBOMService, SimaticBOMService>();
        c.AddSingleton<ISimaticMaterialService, SimaticMaterialService>();
        c.AddSingleton<ISimaticMTUService, SimaticMTUService>();
        c.AddSingleton<ISimaticOrderService, SimaticOrderService>();
        c.AddSingleton<ISimaticWorkOrderCompletionService, SimaticWorkOrderCompletionService>();
        c.AddSingleton<ISimaticEquipmentService, SimaticEquipmentService>();
        // c.AddSingleton<ISimaticRoadMapService, SimaticRoadMapService>();
        // c.AddSingleton<IQualityService, QualityService>();
        // c.AddSingleton<IBulkReceptionService, BulkReceptionService>();
        // c.AddSingleton<IPersonService, PersonService>();

      });
    }
  }

}

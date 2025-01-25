using System;
using System.Linq;
using Endor.Core.Booststrapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nancy.Owin;
using Endor.Core.Logging;
using Nancy;
using Nancy.Bootstrapper;

namespace Endor.Core
{
	public class Startup : StartupBase
	{
		public override void Configure(IApplicationBuilder app)
		{
			app.UseCors(builder =>
				builder
					.AllowAnyOrigin()
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials()
			);

			var bootstrapper = app.ApplicationServices.GetService<INancyBootstrapper>();

			app.UseOwin(pipeline =>
			{
        pipeline.UseNancy(options => options.Bootstrapper = bootstrapper ?? new NancyBootstrapper());
      });
			
			var env = app.ApplicationServices.GetService<IHostingEnvironment>();
			
			var logger = ApplicationLogging.CreateLogger<Startup>();
			var addresses = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses ?? Enumerable.Empty<string>();
			foreach (var address in addresses)
			{
				logger.LogInformation("Listening on [{0}]", address);
			}
			logger.LogInformation("Environment [{0}]", env.EnvironmentName);

		}

		public override void ConfigureServices(IServiceCollection services)
		{
			services.AddCors();
			services.AddLogging((builder) =>
			{
				//builder.AddSerilog(dispose: true);
			});
		}
	}
}

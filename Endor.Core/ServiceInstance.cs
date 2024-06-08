using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Endor.Core.Config;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Endor.Core.Host;
using Endor.Core.Logging;

namespace Endor.Core
{
	public class ServiceInstance
	{

		public static IConfiguration Configuration { get; set; }

		public static IWebHost Host { get; set; }
		
		public static void Run(string[] args, IService service, IHostingStartup startHost)
		{
			Configuration = GetConfiguration(args);

			Log.Logger = new LoggerConfiguration()
				.ReadFrom
				.Configuration(Configuration)
				.CreateLogger();

			Log.Information("Starting Microservice... ");

			Log.Information($"Name [{service.Name}] Version [{Assembly.GetEntryAssembly().GetName().Version}]");
			
			var isService = !(Debugger.IsAttached || args.Contains("--console"));

			if (isService)
			{
				var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
				var pathToContentRoot = Path.GetDirectoryName(pathToExe);
				Directory.SetCurrentDirectory(pathToContentRoot);
			}
			else
			{
				Log.Information("Application started.Press Ctrl + C to shut down.");
			}

			var builder = CreateWebHostBuilder(args.Where(arg => arg != "--console").ToArray());

			startHost.Configure(builder);

			Host = builder.Build();

			service.ServiceStatus = ServiceStatus.Created;
			ApplicationLogging.LoggerFactory = Host.Services.GetRequiredService<ILoggerFactory>();
			
			var appLifetime = Host.Services.GetRequiredService<IApplicationLifetime>();
			appLifetime.ApplicationStarted.Register(() =>
			{
				Log.Information("After OnStart() has been reached");
				service.ServiceStatus = ServiceStatus.Started;
				service.AfterStart();
			});
			appLifetime.ApplicationStopping.Register(() =>
				{
					Log.Information("Calling OnStop()");
					service.BeforeStop();
				}
			);
			appLifetime.ApplicationStopped.Register(() => {

				Log.Information("After OnStop() has been reached");
				service.ServiceStatus = ServiceStatus.Stopped;
				service.AfterStop();
			});

			Log.Information("Calling OnStart()");
			service.BeforeStart();

			if (isService)
			{
				// To run the app without the CustomWebHostService change the
				// next line to host.RunAsService();
				Host.RunAsCustomService();
			}
			else
			{
				Host.Run();
			}

		}

		public static IConfiguration GetConfiguration(string[] args)
		{
			return new ConfigurationBuilder()
				.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args)
		{
			var host = new WebHostBuilder()
				.UseConfiguration(Configuration)
				.UseSerilog()
				//use healthcheck
				.UseKestrel(options =>
        {
          options.Configure(Configuration.GetSection("Kestrel"));
          options.AddServerHeader = false;
        })

        .UseStartup<Startup>()
				.SuppressStatusMessages(true);

			return host;
		}

	}
}

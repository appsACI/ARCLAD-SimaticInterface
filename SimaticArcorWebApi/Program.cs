using Endor.Core;
using Endor.Core.Host;
using System.Reflection;
using Endor.Core.Config;
using Nancy.Swagger.Services;

namespace SimaticArcorWebApi
{
	public class Program : IService
	{
		public static void Main(string[] args)
		{
			ServiceInstance.Run(args, new Program(), new Startup());
		}

		public ServiceStatus ServiceStatus { get; set; }

		public string Name => "Simatic API interface";

		public void AfterStart()
		{
			SwaggerMetadataProvider.SetInfo(
				Name,
				Assembly.GetEntryAssembly().GetName().Version.ToString(),
				"API con Simatic IT UA"
			);
			
		}

		public void AfterStop()
		{

		}

		public void BeforeStart()
		{

		}

		public void BeforeStop()
		{

		}
	}
}

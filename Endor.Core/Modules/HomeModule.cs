using System;
using System.Collections.Generic;
using System.Text;
using Endor.Core.Config;
using Microsoft.Extensions.Configuration;
using Nancy;

namespace Endor.Core.Modules
{
	public class HomeModule : NancyModule
	{
		public ServiceConfig AppConfig { get; set; }
		public HomeModule(IConfiguration config) : base("api/doc")
		{
			AppConfig = new ServiceConfig();
			config.GetSection("ServiceAppConfig").Bind(AppConfig);
			
			Get("/", args =>
			{

				return Response.AsJson(new
				{
					name = AppConfig.Name,
					description = AppConfig.Description,
				});
			});
		}
	}
}

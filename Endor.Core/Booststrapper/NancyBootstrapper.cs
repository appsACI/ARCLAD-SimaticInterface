using System;
using System.Collections.Generic;
using System.Reflection;
using Endor.Core.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Conventions;
using Nancy.TinyIoc;
using Nancy.ViewEngines;

namespace Endor.Core.Booststrapper
{
	public class NancyBootstrapper : DefaultNancyBootstrapper
	{
		protected override void ConfigureApplicationContainer(TinyIoCContainer container)
		{
      
			base.ConfigureApplicationContainer(container);

			var config = ServiceInstance.Host.Services.GetRequiredService<IConfiguration>();
      var appConfig = new ServiceConfig();
      config.GetSection("ServiceAppConfig").Bind(appConfig);

      if (appConfig.BootstrapperAssemblies != null)
      {
        var assemblies = new List<Assembly>();
        foreach (string assemblyName in appConfig.BootstrapperAssemblies)
        {
          var assembly = Assembly.Load(new AssemblyName(assemblyName));

          assemblies.Add(assembly);

          ResourceViewLocationProvider
            .RootNamespaces
            .Add(assembly, "views");
        }

        container.AutoRegister(assemblies);
      }

      container.Register<IConfiguration>(config);
		}

		protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);
			nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/swagger-ui/dist"));
      nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/Script"));
    }
	}
}
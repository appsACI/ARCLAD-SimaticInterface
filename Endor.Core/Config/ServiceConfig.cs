namespace Endor.Core.Config
{
	public class ServiceConfig
	{
		public string Name { get; set; }

		public string Description { get; set; }

    public string[] BootstrapperAssemblies { get; set; }
	}
}
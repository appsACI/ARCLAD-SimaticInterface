using Endor.Core.Config;

namespace Endor.Core.Host
{
	public interface IService
	{
		ServiceStatus ServiceStatus { get; set; }

		string Name { get; }

		void AfterStart();

		void AfterStop();

		void BeforeStart();

		void BeforeStop();

	}
}
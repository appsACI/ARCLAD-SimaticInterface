using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Endor.Core.Logging;

namespace Endor.Core.Jobs
{
	public abstract class JobService : BackgroundService
	{
		public ILogger<JobService> log { get; set; }

		public CancellationToken Token { get; set; }

		public abstract string Name { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var runnnigTask = RunAsync(stoppingToken);
			log.LogInformation("Job service [{0}] started.", Name);
			await runnnigTask;
		}


		public override Task StartAsync(CancellationToken cancellationToken)
		{
			if (log == null) log = ApplicationLogging.CreateLogger<JobService>();
			Token = cancellationToken;
			log.LogInformation("Trying to start job. Name [{0}]", this.Name);
			return base.StartAsync(cancellationToken);

		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			log.LogInformation("Trying to stop job. Name [{0}]", this.Name);
			return base.StopAsync(cancellationToken);
		}

		public abstract Task RunAsync(CancellationToken stoppingToken);
	}
}

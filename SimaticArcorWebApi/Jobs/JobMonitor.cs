using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Jobs;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Config;

namespace SimaticArcorWebApi.Jobs
{
    public class JobMonitor : JobService
    {
        public override string Name => "Monitoring Job";
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<JobMonitor> logger;

        public JobMonitorConfig Config { get; set; }

        public JobMonitor(IConfiguration config)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<JobMonitor>();

            Config = new JobMonitorConfig();
            config.GetSection("JobMonitorConfig").Bind(Config);
        }

        public override async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (Process current = Process.GetCurrentProcess())
                {
                    log.LogInformation($"Private Memory Alloc: [{current.PrivateMemorySize64 / 1024 / 1024}] Mb");
                    log.LogInformation($"Physical Memory Alloc: [{current.WorkingSet64 / 1024 / 1024}] Mb");
                    log.LogInformation($"Virtual Memory Alloc: [{current.VirtualMemorySize64 / 1024 / 1024}] Mb");
                    log.LogInformation($"Threads Count: [{current.Threads.Count}]");
                    log.LogInformation($"Max Physic Memory Used: [{current.PeakWorkingSet64 / 1024 / 1024}] Mb");
                }

                await Task.Delay(Config.Interval, token);
            }

        }
    }
}

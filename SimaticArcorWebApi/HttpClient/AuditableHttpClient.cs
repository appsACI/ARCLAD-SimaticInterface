using System;
using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core;
using Endor.Core.Config;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Configuration;
using SimaticArcorWebApi.Model.Simatic;

namespace SimaticArcorWebApi.HttpClient
{
    public class AuditableHttpClient : System.Net.Http.HttpClient
    {
        public ILogger Log { get; set; }

        public AuditableHttpClient(ILogger log, DelegatingHandler client) : base(client)
        {
            this.Log = log;
            SetBaseConfig();
        }

        public AuditableHttpClient(ILogger log, HttpMessageHandler handler) : base(handler)
        {
            this.Log = log;
            SetBaseConfig();
        }

        public AuditableHttpClient(ILogger log) : base()
        {
            this.Log = log;
            SetBaseConfig();
        }

        public AuditableHttpClient() : base()
        {
            this.Log = ApplicationLogging.CreateLogger<AuditableHttpClient>(); ;
            SetBaseConfig();
        }

        public Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content,
                CancellationToken cancellationToken)
        {
            return CallAuditableTask(() => base.PutAsync(requestUri, content, cancellationToken), requestUri);
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return CallAuditableTask(() => base.PostAsync(requestUri, content, cancellationToken), requestUri);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return CallAuditableTask(() => base.GetAsync(requestUri, cancellationToken), requestUri);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return CallAuditableTask(() => base.SendAsync(request), request.RequestUri.ToString());
        }

        public async Task<T> CallAuditableTask<T>(Func<Task<T>> func, string uri)
        {
            var stopWatch = Stopwatch.StartNew();
            dynamic result = await func();
            stopWatch.Stop();

            Log.LogInformation("Uri {0} Method {1} Result [{2}] took {3} ms",
                uri, func.Method.Name, (string)result.ReasonPhrase?.ToString(), stopWatch.ElapsedMilliseconds);

            return result;
        }

        private void SetBaseConfig()
        {
            var config = ServiceInstance.Configuration;

            if (config == null)
                return;

            var simaticConfig = new SimaticConfig();

            config.GetSection("SimaticConfig").Bind(simaticConfig);

            if (simaticConfig.Timeout.HasValue)
                this.Timeout = new TimeSpan(0, 0, simaticConfig.Timeout.GetValueOrDefault());
            //var config = (IConfiguration).GetService(typeof(IConfiguration));

        }

    }
}
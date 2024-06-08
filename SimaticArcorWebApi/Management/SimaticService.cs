using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MaterialLot;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.RoadMap;

namespace SimaticArcorWebApi.Management
{
    public class SimaticService : ISimaticService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticService> logger;

        /// <summary>
        /// The task configuration class
        /// </summary>
        protected SimaticConfig configuration;

        /// <summary>
        /// The Simatic IT UA access token to handle expiration times
        /// </summary>
        protected AccessToken accessToken;

        #endregion

        public IUOMService UomService { get; set; }

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticService"/> class.
        /// </summary>
        public SimaticService(IConfiguration config, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticService>();

            configuration = new SimaticConfig();
            config.GetSection("SimaticConfig").Bind(configuration);

            logger.LogInformation("Simatic api path: {0}", configuration.Url);

            UomService = uomService;

            accessToken = new AccessToken { ExpirationDate = DateTime.MinValue };
        }
        #endregion

        #region Interface Implementation

        public async Task<string> GetAccessToken(CancellationToken token, bool force = false)
        {
            if (force || accessToken.ExpirationDate < DateTime.Now)
            {
                await RenewAuthorizationTokenAsync(token);
            }

            return accessToken.Value;
        }

        public string GetUrl()
        {
            return configuration.Url;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Renew token authorization with the api mobile client
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task RenewAuthorizationTokenAsync(CancellationToken token)
        {
            var apiToken = await CallApiToGetAccessToken(token);
            accessToken.ExpirationDate = (DateTime.Now).AddSeconds(apiToken.ExpiresIn - 5); //5 seconds, Delta safety time to process avoid the invalid grant while process the operation call
            accessToken.Value = apiToken.AccessToken;
            logger.LogInformation("Access token was updated. Value [{0}], Expiration Date [{1}]", accessToken.Value, accessToken.ExpirationDate);
        }

        /// <summary>
        /// This method uses the OAuth Client Credentials Flow to get an Access Token to provide
        /// Authorization to the APIs.
        /// </summary>
        /// <returns></returns>
        protected async Task<SimaticToken> CallApiToGetAccessToken(CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(this.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                  "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{configuration.Username}:{configuration.Password}")));

                var dict = new Dictionary<string, string>
        {
          { "grant_type", configuration.GrantType },
          { "username", configuration.Username },
          { "password", configuration.Password },
          { "scope", configuration.Scope }
        };

                // Post to the Server and parse the response.
                HttpResponseMessage response = await client.PostAsync("sit-auth/OAuth/Token", new FormUrlEncodedContent(dict), token);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await response.Content.ReadAsStringAsync()
                      .ContinueWith(task =>
                        logger.LogError(task.Result), token);

                    throw new GettingAccessTokenException(response.ReasonPhrase);
                }

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.ToObject<SimaticToken>();
                  }, token);
            }
        }

        #endregion

    }
}

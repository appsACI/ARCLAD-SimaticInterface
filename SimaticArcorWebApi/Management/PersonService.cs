using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom.Person;

namespace SimaticArcorWebApi.Management
{
    public class PersonService : IPersonService
    {
        #region Fields
        private ILogger<PersonService> logger;
        public ISimaticService SimaticService { get; set; }
        #endregion

        #region Constructor

        public PersonService(ISimaticService simatic)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<PersonService>();

            SimaticService = simatic;
        }

        #endregion

        public async Task CreatePerson(CreatePersonRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                // Build up the data to POST.

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        PlantID = req.plantID,
                        StartTime = req.startTime,
                        PersonnelFile = req.personnel
                    }
                });

                var response = await client.PostAsync("sit-svc/application/LineProductionApp/odata/SavePersonnelHours",
                    new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
    }
}

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
using SimaticArcorWebApi.Model.Custom.Quality;

namespace SimaticArcorWebApi.Management
{
    public class QualityService : IQualityService
    {
        #region Fields
        private ILogger<QualityService> logger;
        public ISimaticService SimaticService { get; set; }
        #endregion

        #region Constructor

        public QualityService(ISimaticService simatic)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<QualityService>();

            SimaticService = simatic;
        }

        #endregion

        #region Interface Implementation

        public async Task CreateProductSpecification(CreateProductSpecificationRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                logger.LogInformation($"Creating a specificatinon, ID [{req.IdProtocol}] Final Material [{req.Definition}]");

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
                        SampleType = new
                        {
                            IdProtocol = req.IdProtocol,
                            Revision = req.Revision,
                            IdPlant = req.IdPlant,
                            Definition = req.Definition,
                            WC = req.WC,
                            Frequency = req.Frequency,
                            Sample = req.Sample,
                            UoMF = req.UoMF,
                            Properties = req.Properties,
                            DescProtocol = req.descProtocol
                        }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/RnDSuiteAp/odata/RnDSuiteCreateSampleType",
                    new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task CreateSample(CreateSampleRequest req, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                logger.LogInformation($"Creating a sample, ID [{req.SampleId}] for protocol [{req.IdProtocol}]");

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
                        Definition = req.Definition,
                        Lot = req.Lot,
                        IdCarga = req.IdCarga,
                        Datetime = req.Datetime,
                        IdPlant = req.IdPlant,
                        SampleId = req.SampleId,
                        IdProtocol = req.IdProtocol,
                        Revision = req.Revision,
                        Properties = req.Properties.Select(s => new
                        {
                            Name = s.Name,
                            Type = s.Type,
                            PropValue = s.Value,
                            ActualValue = s.ActualValue,
                            UoM = s.UoM
                        }).ToList()
                    }
                });

                var response = await client.PostAsync("sit-svc/application/RnDSuiteAp/odata/RnDSuiteCreateSampleWithParameters",
                    new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
        
        #endregion
    }
}

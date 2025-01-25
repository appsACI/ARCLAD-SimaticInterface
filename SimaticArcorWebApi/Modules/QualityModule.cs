using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom.Quality;
using SimaticArcorWebApi.Validators.Quality;

namespace SimaticArcorWebApi.Modules
{
    public class QualityModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<QualityModule> logger;

        private NancyConfig config;

        private QualityConfig qConfig;

        /// <summary>
        /// Gets or sets the quality service component
        /// </summary>
        public IQualityService QualityService { get; set; }

        public QualityModule(IConfiguration configuration, IQualityService qualityService) : base("api/quality")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<QualityModule>();

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            qConfig = new QualityConfig();
            configuration.GetSection("QualityConfig").Bind(qConfig);

            this.QualityService = qualityService;

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            Post("/sample", CreateSample, name: "CreateSample");

            Post("/productspecification", CreateProductSpecification, name: "CreateProductSpecification");

            Post("/productspecification/bulkInsert", CreateProductSpecificationBulkInsert, name: "CreateProductSpecificationBulkInsert");

        }

        #region Post

        private async Task<dynamic> CreateSample(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                if (!qConfig.SampleEnabled)
                {
                    logger.LogWarning($"Sample not enabled returning OK!!!");
                    return Negotiate.WithStatusCode(HttpStatusCode.Created);
                }

                var req = this.BindAndValidate<CreateSampleRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await QualityService.CreateSample(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create sample");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create sample");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> CreateProductSpecification(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                if (!qConfig.SpecEnabled)
                {
                    logger.LogWarning($"Sepecification not enabled returning OK!!!");
                    return Negotiate.WithStatusCode(HttpStatusCode.Created);
                }

                var req = this.BindAndValidate<CreateProductSpecificationRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await QualityService.CreateProductSpecification(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create product specification");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create product specification");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }



        private async Task<dynamic> CreateProductSpecificationBulkInsert(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                if (!qConfig.SpecEnabled)
                {
                    logger.LogWarning($"Sepecification not enabled returning OK!!!");
                    return Negotiate.WithStatusCode(HttpStatusCode.Created);
                }

                var productList = this.BindAndValidate<CreateProductSpecificationRequest[]>();
                var errorList = new List<KeyValuePair<string, string>>();

                foreach (var product in productList)
                {
                    try
                    {
                        var validator = new ProductSpecificationRequestValidator();
                        var valResult = validator.Validate(product);
                        if (!valResult.IsValid)
                        {
                            throw new Exception(string.Join('\n', valResult.Errors));
                        }

                        await QualityService.CreateProductSpecification(product, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error trying to create producto specification. IdProtocol: [{product.IdProtocol}]. Definition: [{product.Definition}]. Revision: [{product.Revision}] Message: [{e.Message}]");
                        errorList.Add(new KeyValuePair<string, string>(product.IdProtocol, e.Message));
                    }
                }
                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(errorList);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create product specification");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create product specification");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion
    }
}

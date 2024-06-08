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
using SimaticArcorWebApi.Model.Custom.Person;
using SimaticArcorWebApi.Validators.Person;

namespace SimaticArcorWebApi.Modules
{
    public class PersonModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<PersonModule> logger;

        private NancyConfig config;

        /// <summary>
        /// Gets or sets the quality service component
        /// </summary>
        public IPersonService PersonService { get; set; }


        public PersonModule(IConfiguration configuration, IPersonService personService) : base("api/personnel")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<PersonModule>();

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            this.PersonService = personService;

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });


            Post("/RegisterPlantEntry/BulkInsert", CreatePersonBulkInsert, name: "CreatePersonBulkInsert");

        }


        private async Task<dynamic> CreatePersonBulkInsert(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var personList = this.BindAndValidate<CreatePersonRequest[]>();
                var errorList = new List<KeyValuePair<string, string>>();

                foreach (var person in personList)
                {
                    try
                    {
                        var validator = new PersonPropertyValidator();
                        var valResult = validator.Validate(person);
                        if (!valResult.IsValid)
                        {
                            throw new Exception(string.Join('\n', valResult.Errors));
                        }

                        await PersonService.CreatePerson(person, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error trying to create personnel. PersonnelFile: [{person.personnel}]. Message: [{e.Message}]");
                        errorList.Add(new KeyValuePair<string, string>(person.personnel, e.Message));
                    }
                }
                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(errorList);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create personnel");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create personnel");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }
    }
}

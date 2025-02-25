using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using Serilog;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Modules;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.G8Response;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Modules.DCMovement;
using SimaticArcorWebApi.Validators.Material;
using SimaticArcorWebApi.Validators.Order;
using SimaticArcorWebApi.Validators.MTU;
using SimaticWebApi.Model.Custom.PrintLabel;
using SimaticWebApi.Model.Custom.RDL;

namespace SimaticArcorWebApi.Modules
{
    public class MTUModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<MTUModule> logger;

        public IOrderService OrderService { get; set; }

        private NancyConfig config;

        /// <summary>
        /// Gets or sets the mtu service component
        /// </summary>
        public IMTUService MTUService { get; set; }

        public ISimaticMTUService SimaticService { get; set; }

        public MTUModule(IConfiguration configuration, IOrderService orderService, IMTUService mtuService, ISimaticMTUService simaticService) : base("api/mtus")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<MTUModule>();

            this.OrderService = orderService;

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            this.MTUService = mtuService;
            this.SimaticService = simaticService;

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            Post("/", UpdateLot, name: "UpdateLot");

            Post("/updateVinilos", UpdateLotVinilos, name: "UpdateLotVinilos");

            Post("/descount", Descount, name: "Descount");

            Post("/AssignedUnassign", AssignedUnassign, name: "AssignedUnassign");

            Post("/printLabel", PrintLabel, name: "PrintLabel");

            Post("/printLabel/Bulk", PrintLabelBulk, name: "PrintLabelBulk");

            Post("/Bulk", UpdateLotBulk, name: "UpdateLotBulk");

            Post("/MoveLot", MoveMTUAsync, name: "MoveMTU");

            Put("/G8Response", G8Response, name: "G8Response");

            Post("/DCMovement", DCMovement, name: "G8Response");

            Post("/LotMovementJDE", LotMovementJDE, name: "LotMovementJDE");

            Post("/GetPropertiesDefectos", GetPropertiesDefectosAsync, name: "GetPropertiesDefectos");

            Post("/LabelHistory", LabelHistoryAsync, name: "LabelHistory");

            Get("/GetMaterialTrackingUnitProperty/{NId}", GetMaterialTrackingUnitProperty, name: "GetMaterialTrackingUnitProperty");

            Get("/ConsultaRequirement/{Lote}/{Nid}", ConsultaRequirement, name: "ConsultaRequirement");

            Post("/createSample", CreateSample, name: "CreateSample");

            Post("/createRequirement", CreateRequirement, name: "CreateRequirement");

            Post("/updateRequirement", UpdateRequirement, name: "UpdateRequirement");

            Post("/createRequirementCorte", CreateRequirementCorte, name: "CreateRequirementCorte");

            Post("/UpdateRequirementLength", UpdateRequirementLength, name: "UpdateRequirementLength");

        }

        #region Get

        public async Task<dynamic> GetMaterialTrackingUnitProperty(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                string NId = ((dynamic)parameters).NId.Value;
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{NId}] WorkOrder [{NId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{NId}";
                var href = $"{Request.Path}/{NId}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                IList<MaterialTrackingUnitProperty> materialTrackingUnitProperty = await OrderService.GetParametersMTU(NId, ct);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(materialTrackingUnitProperty);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #region RDL

        private async Task<dynamic> ConsultaRequirement(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Create Requirement:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                string Lote = ((dynamic)parameters).Lote.Value;
                string Nid = ((dynamic)parameters).Nid.Value;
                logger.LogInformation($"parameters:[{((dynamic)parameters).Nid}]");
                logger.LogInformation($"Lote:[{Lote}], Material:[{Nid}]");

                var response = await MTUService.ConsultaRequirement(Lote, Nid, ct);
                logger.LogInformation($"Response:[{response}]");
                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = $"Response:[{response}]" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to Found requirement");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to Found requirement");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion

        #endregion

        #region Put

        private async Task<dynamic> G8Response(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<G8ResponseRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                MaterialTrackingUnit mtu = await SimaticService.GetMTUAsync(req.Id, ct);

                if (mtu == null) return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel($"MTU not found ID [{req.Id}]");

                var prop = req.MaterialLotProperty.FirstOrDefault(t => t.Id == "PosicionCD");

                if (prop == null) return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel($"PosicionCD was not found MTU [{req.Id}]");

                var mtuProp = new MTURequestMaterialLotProperty()
                {
                    Id = prop.Id,
                    PropertyValue = new MTURequestPropertyValue() { Type = prop.Value.DataType, ValueString = prop.Value.ValueString.ToString(), UnitOfMeasure = prop.Value.UnitOfMeasure }
                };

                await MTUService.CreateOrUpdateMTUProperties(mtu.Id, new MTURequestMaterialLotProperty[] { mtuProp }, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Updates or creates Lot & MTU in Bulk
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> UpdateLotBulk(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body update lote bulk:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var mtuList = this.Bind<MTURequest[]>();
                var errorList = new List<KeyValuePair<string, string>>();

                foreach (var mtu in mtuList)
                {
                    try
                    {
                        var validator = new MTUValidator();
                        var valResult = validator.Validate(mtu);
                        if (!valResult.IsValid)
                        {
                            throw new Exception(string.Join('\n', valResult.Errors));
                        }

                        await MTUService.UpdateLotAsync(mtu, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error trying to Update MTU. Id: [{mtu.Id}] Message: [{e.Message}]");
                        errorList.Add(new KeyValuePair<string, string>(mtu.Id, e.Message));
                    }
                }

                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                        errorList.Select(i => new { Id = i.Key, Message = i.Value }));

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        /// <summary>
        /// Updates or creates Lot & MTU
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> Descount(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                var errorList = new List<KeyValuePair<string, string>>();

                var req = this.BindAndValidate<MTUDescount>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                try
                {

                    await MTUService.DescountQuantity(req, ct);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Error trying to Update MTU. Id: [{req.MtuInfo.ToDynamic()}] Message: [{e.Message}]");
                    errorList.Add(new KeyValuePair<string, string>(req.MtuInfo.ToDynamic(), e.Message));
                }

                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                        errorList.Select(i => new { Id = i.Key, Message = i.Value }));

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> AssignedUnassign(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                logger.LogError("validando modelo");

                var req = this.BindAndValidate<MTUAssignedUnassign>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                logger.LogError("modelo validado");

                await MTUService.AssignedUnassign(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying Unsiggned mtu");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying Unsiggned mtu");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> GetPropertiesDefectosAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                var req = this.BindAndValidate<MTUId>();

                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{req.Id}] WorkOrder [{req.Id}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{req.Id}";
                var href = $"{Request.Path}/{req.Id}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                IList<MTUPropiedadesDefectos> PropidadesDefectosMTU = await MTUService.GetPropertiesDefectos(req.Id, tokenSource.Token);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(PropidadesDefectosMTU);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create order");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        /// <summary>
        /// Updates or creates Lot & MTU
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> PrintLabel(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Print Label:{ RequestStream.FromStream(Request.Body).AsString()}");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<PrintModel>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);


                await MTUService.PrintLabel(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        /// <summary>
        /// Updates or creates Lot & MTU
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> PrintLabelBulk(dynamic parameters, CancellationToken ct)
        {

            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body print label bulk:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var PrintList = this.Bind<PrintModel[]>();
                var errorList = new List<KeyValuePair<string, string>>();

                foreach (var print in PrintList)
                {
                    try
                    {
                        await MTUService.PrintLabel(print, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Error trying to Update MTU. Id: [{print.LabelTagsMezclas.TagMaterial}] Message: [{e.Message}]");
                        errorList.Add(new KeyValuePair<string, string>(print.LabelTagsMezclas.TagMaterial, e.Message));
                    }
                }

                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                        errorList.Select(i => new { Id = i.Key, Message = i.Value }));

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to print label");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to print label");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }

        }

        public async Task<dynamic> LabelHistoryAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                var req = this.BindAndValidate<ReimpresionFilters>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);


                CancellationTokenSource tokenSource = new CancellationTokenSource();

                IList<LabelHistory> labelHistories = await MTUService.GetLabelHistory(req, ct);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(labelHistories);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to get history");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to get print history");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }   

        /// <summary>
        /// Updates or creates Lot & MTU
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> UpdateLot(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body update lot:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<MTURequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await MTUService.UpdateLotAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> UpdateLotVinilos(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body update lot Vinilos:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<MTURequest[]>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await MTUService.UpdateLotVinilosAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        /// <summary>
        /// Moves part or whole MTU to location
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> MoveMTUAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<MTURequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await MTUService.MoveMTUAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to move lot");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to move lot");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> DCMovement(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<DCMovementRequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await SimaticService.DCMovement(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to send data to system");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to send data to system");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        /// <summary>
        /// Updates or creates Lot & MTU
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<dynamic> LotMovementJDE(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<MTURequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await MTUService.LotMovementJDE(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to move lot from/to JDE");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to move lot from/to JDE");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #region RDL

        private async Task<dynamic> CreateSample(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body CreateSample:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<MTURequest>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await MTUService.UpdateLotAsync(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = "Creada o actualizada correctamente" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update mtu");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update mtu");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> CreateRequirement(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Create Requirement:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<CreateRequirementModel>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                var response = await MTUService.CreateRequirement(req, ct);
                logger.LogInformation($"Response:[{response}]");
                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = "Creada correctamente" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> UpdateRequirement(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Create Requirement:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<CreateRequirementModel>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                var response = await MTUService.UpdateRequirement(req, ct);
                logger.LogInformation($"Response:[{response}]");
                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = "Actualizada correctamente" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> CreateRequirementCorte(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Create Requirement:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<CreateRequirementModelCorte>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                var response = await MTUService.CreateRequirementCorte(req, ct);
                logger.LogInformation($"Response:[{response}]");
                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = "Creada o actualizada correctamente" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        private async Task<dynamic> UpdateRequirementLength(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body Create Requirement:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<UpdateLengthModel>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                var response = await MTUService.UpdateRequirementLength(req, ct);
                logger.LogInformation($"Response:[{response}]");
                return Negotiate.WithStatusCode(HttpStatusCode.Created).WithModel(new { message = "Creada o actualizada correctamente" }); ;
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create requirement");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion

        #endregion
    }



}

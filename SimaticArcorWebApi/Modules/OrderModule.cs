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
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticArcorWebApi.Validators.Order;
using SimaticWebApi.Model.Custom.Counters;

namespace SimaticArcorWebApi.Modules
{
    public class OrderModule : NancyModule
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<OrderModule> logger;

        private NancyConfig config;

        /// <summary>
        /// Gets or sets the material service component
        /// </summary>
        public IOrderService OrderService { get; set; }

        /// <summary>
        /// Gets or sets the simatic service component
        /// </summary>
        public ISimaticOrderService SimaticService { get; set; }

        public OrderModule(IConfiguration configuration, IOrderService orderService, ISimaticOrderService simaticService) : base("api/orders")
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<OrderModule>();

            this.OrderService = orderService;
            this.SimaticService = simaticService;

            config = new NancyConfig();
            configuration.GetSection("NancyConfig").Bind(config);

            base.Get("/docs", x =>
            {
                var site = Request.Url.SiteBase;
                return Response.AsRedirect($"/swagger-ui/dist/index.html?url={site}/api-docs");
            });

            Post("/", CreateOrdersAsync, name: "CreateOrders");

            //Get("/{NId}", GetWorkOrderStatus, name: "GetOrderStatus");

            Post("/bulkInsert", CreateOrdersBulkInsertAsync, name: "CreateOrdersBulkInsert");


            Post("/operationInsert", CreateOrdersOperationsInsertAsync, name: "CreateOrdersOperations");

            Post("/updateChecksRevisionOperations", UpdateChecksRevisionOperationsAsync, name: "UpdateChecksRevisionOperations");

            Post("/statusAndTime", ChangeStatusAndTime, name: "ChangeStatusAndTime");


            Get("/getParametersRevisionTrim/{NId}", GetParametersRevisionTrimAsync, name: "GetParametersRevisionTrim");

            Get("/getParametersRevisionMTU/{NId}", GetParametersRevisionMTUAsync, name: "GetParametersRevisionMTU");

            Get("/updateCounterTrimCorte/{NId}", UpdateCounterTrimCorteAsync, name: "UpdateCounterTrimCorte");

            Post("/addMaterialOperationEmpaque", AddMaterialOperationEmpaqueAsync, name: "AddMaterialOperationEmpaque");

            Post("/rtdsWrite", PostToRTDSWriteAsync, name: "PostToRTDSWrite");

            Post("/UpdateCounters", UpdateCounters, name: "UpdateCounters");
        }

        #region Get

        public async Task<dynamic> GetParametersRevisionTrimAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                string orderId = ((dynamic)parameters).NId.Value;
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{orderId}] WorkOrder [{orderId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{orderId}";
                var href = $"{Request.Path}/{orderId}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                IList<WorkOrderOperationParameterSpecification> woOperationsParameters = await OrderService.GetParametersRevisionTrim(orderId, tokenSource.Token);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(woOperationsParameters);
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

        public async Task<dynamic> GetParametersRevisionMTUAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                string orderId = ((dynamic)parameters).NId.Value;
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{orderId}] WorkOrder [{orderId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{orderId}";
                var href = $"{Request.Path}/{orderId}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                IList<WorkOrderOperationParameterSpecification> woOperationsParameters = await OrderService.GetParametersRevisionMTU(orderId, tokenSource.Token);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(woOperationsParameters);
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

        public async Task<dynamic> UpdateCounterTrimCorteAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                string parameterId = ((dynamic)parameters).NId.Value;
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{parameterId}] WorkOrder [{parameterId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{parameterId}";
                var href = $"{Request.Path}/{parameterId}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                WorkOrderOperationParameterSpecification woOperationParameters = await OrderService.UpdateCounterTrimCorte(parameterId, tokenSource.Token);

                return await Negotiate.WithStatusCode(HttpStatusCode.OK).WithModel(woOperationParameters);
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

        [Obsolete]
        private async Task<dynamic> GetWorkOrderStatus(dynamic parameters, CancellationToken ct)
        {
            return Negotiate.WithStatusCode(HttpStatusCode.Accepted).WithModel("Obsolete");

            //try
            //{
            //  string orderId = ((dynamic)parameters).NId.Value;
            //  Order order = await SimaticService.GetOrderByNIdAsync(orderId, true, ct);

            //  WorkOrder workOrder = await SimaticService.GetWorkOrderByNIdAsync(orderId, false, ct);

            //  if (order == null)
            //  {
            //    dynamic errorMessage = new { Error = "Order not found." };
            //    throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            //  }

            //  if (workOrder == null)
            //  {
            //    // Significa que la WorkOrder no está creada, puede haber sido por error o por que no se atendió todavia el evento
            //    // ApplicationLog[] orderApplicationLogs = await SimaticService.GetWorkOrderApplicationLogs(order.Id, ct);

            //    // Si hay algun application log de tipo Error (2) o Critical (4), devolver un error
            //    //if (orderApplicationLogs.ToList().Any(a => a.MessageId == 2 || a.MessageId == 4))
            //    //{
            //    //    ApplicationLogMessage[] orderApplicationLogMessages =
            //    //        await SimaticService.GetApplicationLogMessages(orderApplicationLogs.ToList().Where(w => w.MessageId == 2 || w.MessageId == 4).Select(s => s.Id).ToList(),ct);
            //    //    var errorMessageList = orderApplicationLogMessages.ToList().Select(s => s.LongMessage).ToList();
            //    //    dynamic errorMessage = new { Error = errorMessageList.FirstOrDefault() };
            //    //    throw new SimaticApiException(Nancy.HttpStatusCode.BadRequest, errorMessage);
            //    //}

            //    return Negotiate.WithStatusCode(Nancy.HttpStatusCode.Processing);
            //    //    dynamic errorMessage = new { Error = "Work Order not found." };
            //    //throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            //  }

            //  //ApplicationLog[] applicationLogs = await SimaticService.GetWorkOrderApplicationLogs(workOrder.Id, ct);

            //  //// Si hay algun application logs para la work order con tipo de mensaje 2 (error) o 4 (critical), se devuelve un error
            //  //if (applicationLogs.ToList().Any(a => a.MessageId == 2 || a.MessageId == 4))
            //  //{
            //  //    ApplicationLogMessage[] workOrderApplicationLogMessages =
            //  //        await SimaticService.GetApplicationLogMessages(applicationLogs.ToList().Where(w => w.MessageId == 2 || w.MessageId == 4).Select(s => s.Id).ToList(), ct);
            //  //    var errorMessageList = workOrderApplicationLogMessages.ToList().Select(s => s.LongMessage).ToList();
            //  //    dynamic errorMessage = new { Error = errorMessageList.FirstOrDefault() };
            //  //    throw new SimaticApiException(Nancy.HttpStatusCode.BadRequest, errorMessage);
            //  //}

            //  //// Se comparan numéricamente los estados de la orden y de la work order para saber si la work order se creó correctamente
            //  //// OJO: esto solo se puede hacer porque los estados son consecutivos y numéricos!
            //  //if (Convert.ToInt32(order.Status.StatusNId) <= Convert.ToInt32(workOrder.Status.StatusNId)) return Negotiate.WithStatusCode(Nancy.HttpStatusCode.Created);

            //  return Negotiate.WithStatusCode(Nancy.HttpStatusCode.Processing);
            //}
            //catch (SimaticApiException e)
            //{
            //  logger.LogError(e, "Error trying to get order status");
            //  return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            //}
            //catch (Exception e)
            //{
            //  logger.LogError(e, "Error trying to get order status");
            //  return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            //}
        }

        #endregion

        #region Post

        public async Task<dynamic> CreateOrdersBulkInsertAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }
                var productionList = this.BindAndValidate<ProductionRequest[]>();
                var errorList = new List<KeyValuePair<string, string>>();
                foreach (var prod in productionList)
                {

                    try
                    {
                        var validator = new ProductionRequestValidator();
                        var valResult = validator.Validate(prod);
                        if (!valResult.IsValid)
                        {
                            throw new Exception(string.Join('\n', valResult.Errors));
                        }

                        await OrderService.ProcessNewOrderAsync(prod, ct);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error trying to create order. Order Id: [{prod.Id}]");
                        errorList.Add(new KeyValuePair<string, string>(prod.Id, e.Message));
                    }
                }

                if (errorList.Count > 0)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(
                        errorList.Select(i => new { Id = i.Key, Message = i.Value }));

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to create orders");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to create orders");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        public async Task<dynamic> CreateOrdersAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<ProductionRequest>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.Id}] WorkOrder [{prod.WorkOrder}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{prod.Id}";
                var href = $"{Request.Path}/{prod.Id}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                await OrderService.ProcessNewOrderAsync(prod, tokenSource.Token);

                var response = new ProductionResponse()
                {
                    Order = new OrderResponseObject()
                    {
                        Id = prod.Id,
                        href = href
                    }
                };

                return Negotiate.WithStatusCode(HttpStatusCode.Accepted).WithHeader("Location", location).WithModel(response);
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

        public async Task<dynamic> CreateOrdersOperationsInsertAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<WorkOrderOperationRevisionRequest>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.Id}] WorkOrder [{prod.ProcessParameters[0].ParameterNId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{prod.Id}";
                var href = $"{Request.Path}/{prod.Id}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                await OrderService.ProcessNewOperationAsync(prod, tokenSource.Token);

                var response = new ProductionResponse()
                {
                    Order = new OrderResponseObject()
                    {
                        Id = prod.Id,
                        href = href
                    }
                };

                return Negotiate.WithStatusCode(HttpStatusCode.Accepted).WithHeader("Location", location).WithModel(response);
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

        public async Task<dynamic> ChangeStatusAndTime(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<StatusAndTimeOrder>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await OrderService.ChangeStatusAndTimeOrden(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update WorkOrder Status");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update WorkOrder Status");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        public async Task<dynamic> UpdateCounters(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{ RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var req = this.BindAndValidate<Counters>();

                if (!this.ModelValidationResult.IsValid)
                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);

                await OrderService.UpdateWorkOrderCounters(req, ct);

                return Negotiate.WithStatusCode(HttpStatusCode.Created);
            }
            catch (SimaticApiException e)
            {
                logger.LogError(e, "Error trying to update WorkOrder Status");
                return Negotiate.WithStatusCode(e.StatusCode).WithModel(e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error trying to update WorkOrder Status");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        public async Task<dynamic> UpdateChecksRevisionOperationsAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<WorkOrderOperationRevisionRequest>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.Id}] WorkOrder [{prod.ProcessParameters[0].ParameterNId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{prod.Id}";
                var href = $"{Request.Path}/{prod.Id}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                await OrderService.UpdateChecksRevisionOperationsAsync(prod, tokenSource.Token);

                var response = new ProductionResponse()
                {
                    Order = new OrderResponseObject()
                    {
                        Id = prod.Id,
                        href = href
                    }
                };

                return Negotiate.WithStatusCode(HttpStatusCode.Accepted).WithHeader("Location", location).WithModel(response);
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

        public async Task<dynamic> AddMaterialOperationEmpaqueAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var prod = this.BindAndValidate<MaterialOperationEmpaqueCorteHojas>();
                if (!this.ModelValidationResult.IsValid)
                {
                    logger.LogError($"Order [{prod.workOrderOperationId}] WorkOrder [{prod.workOrderOperationId}] structure is not valid!");

                    return Negotiate.WithStatusCode(HttpStatusCode.BadRequest).WithModel(this.ModelValidationResult.FormattedErrors);
                }

                var location = $"{Request.Url}/{prod.workOrderOperationId}";
                var href = $"{Request.Path}/{prod.workOrderOperationId}";

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                await OrderService.AddMaterialOperationEmpaqueCorteHojasAsync(prod, tokenSource.Token);

                var response = new ProductionResponse()
                {
                    Order = new OrderResponseObject()
                    {
                        Id = prod.workOrderOperationId,
                        href = href
                    }
                };

                return Negotiate.WithStatusCode(HttpStatusCode.Accepted).WithHeader("Location", location).WithModel(response);
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

        private async Task<dynamic> PostToRTDSWriteAsync(dynamic parameters, CancellationToken ct)
        {
            try
            {
                if (config.EnableRequestLogging)
                {
                    logger.LogInformation($"Body:[{RequestStream.FromStream(Request.Body).AsString()}]");
                    Request.Body.Position = 0;
                }

                var requestBody = this.Bind<RTDS>();
                var response = await SimaticService.PostToRTDSWriteAsync(requestBody, ct);

                var responseContent = await response.Content.ReadAsStringAsync();
                var statusCode = (int)response.StatusCode;
                return Negotiate.WithStatusCode(statusCode).WithModel(responseContent);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during POST to RTDSWrite");
                return Negotiate.WithStatusCode(HttpStatusCode.InternalServerError).WithModel(e.Message);
            }
        }

        #endregion
    }
}

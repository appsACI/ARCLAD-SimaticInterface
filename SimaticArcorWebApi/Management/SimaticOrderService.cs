using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticWebApi.Model.Custom.Counters;
using SimaticWebApi.Model.Custom.Reproceso;

namespace SimaticArcorWebApi.Management
{
    public class SimaticOrderService : ISimaticOrderService
    {
        #region Fields
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<SimaticOrderService> logger;

        #endregion

        public ISimaticService SimaticService { get; set; }

        public IUOMService UomService { get; set; }

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SimaticOrderService"/> class.
        /// </summary>
        public SimaticOrderService(ISimaticService simatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticOrderService>();

            SimaticService = simatic;
            UomService = uomService;
        }
        #endregion

        #region Interface Implementation

        public async Task<string> CreateOrderAsync(ProductionRequest orderData, Material material, IOOrderUserField[] userFields, IOOrderOperation[] orderOperations, CancellationToken ct)
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
                        Order = new
                        {
                            PlannedStartTime = orderData.StartTime,
                            PlannedEndTime = orderData.EndTime,
                            Description = material.Description,
                            EquipmentNId = orderData.Operations[0].Asset,
                            MaterialNId = material.NId,
                            Name = orderData.Id,
                            NId = orderData.WorkOrder,
                            Quantity = new { QuantityValue = orderData.Quantity, material.UoMNId },
                            StateMachineNId = "WorkOrder",
                            Type = orderData.OrderType,
                            BoMNId = orderData.BomId,
                            BoMRevision = "",
                            OrderUserFields = userFields,
                            OrderOperations = orderOperations,
                        }
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIInteroperability/odata/IOCreateOrder",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.OrderId.ToString();
                  }, ct);
            }
        }

        public async Task<string> AddMaterialReprosesoWorkOrderAsync(WorkOrderOperationMaterialRequirement[] materiales, string workOrderOperationId, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));


                var json = JsonConvert.SerializeObject(new
                {

                    command = new WorkOrderReproceso()
                    {
                        WorkOrderOperationId = workOrderOperationId,
                        WorkOrderOperationMaterialRequirements = materiales
                    }
                });


                var response = await client.PostAsync("sit-svc/Application/PICore/odata/AddWorkOrderOperationMaterialRequirementsToWorkOrderOperation",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> AddMaterialReprosesoOrderAsync(OrderOperationMaterialRequirement[] materiales, string orderOperationId, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));


                var json = JsonConvert.SerializeObject(new
                {

                    command = new OrderReproceso()
                    {
                        OrderOperationId = orderOperationId,
                        OrderOperationMaterialRequirements = materiales
                    }
                });
                logger.LogInformation($"Json {json}");

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/AddOrderOperationMaterialRequirementsToOrderOperation",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> AddWorkOrderPriority(string id, int priority, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));


                var json = JsonConvert.SerializeObject(new
                {

                    command = new
                    {
                        Id = id,

                        Priority = priority
                    }
                });
                logger.LogInformation($"Json {json}");

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }


        public async Task<string> UpdateOrderAsync(string id, string name, string description, DateTime? actualStartTime, DateTime? actualEndTime,
          string equipment, string materialId, string materialRevision, DateTime startTime, DateTime endTime, string stateMachine,
          decimal quantity, string unitOfMeasure, string type, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(ct));

                dynamic prop = new ExpandoObject();
                prop.Id = id;
                if (name != null) prop.Name = name;
                if (description != null) prop.Description = description;
                if (actualStartTime != null) prop.ActualStartTime = actualStartTime;
                if (actualEndTime != null) prop.ActualEndTime = actualEndTime;
                if (equipment != null) prop.EquipmentId = equipment;
                if (materialId != null) prop.MaterialNId = materialId;
                if (materialRevision != null) prop.MaterialRevision = materialRevision;
                prop.PlannedStartTime = startTime;
                prop.PlannedEndTime = endTime;
                if (stateMachine != null) prop.StateMachineNId = stateMachine;
                prop.Quantity = new { UoMNId = UomService.GetSimaticUOM(unitOfMeasure), QuantityValue = quantity };
                if (type != null) prop.Type = type;

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {

                    command = new
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        ActualStartTime = actualStartTime,
                        ActualEndTime = actualEndTime,
                        EquipmentId = equipment,
                        MaterialNId = materialId,
                        MaterialRevision = materialRevision,
                        PlannedStartTime = startTime,
                        PlannedEndTime = endTime,
                        StateMachineNId = stateMachine,
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(unitOfMeasure), QuantityValue = quantity },
                        Type = type
                    }
                });

                var response = await client.PostAsync("sit-svc/application/PICore/odata/UpdateOrder",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> GetApplicationLogMessages(IList<string> ids, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // TODO: ver como pasar toda la lista de ids en la llamada a la RF
                var url = $"sit-svc/application/ApplicationLog/odata/ApplicationLog_FetchApplicationLogMessages(function=@x)?@x={{\"Id\": [\"{ids.FirstOrDefault()}\"]}}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                        return result.value.ToObject<ApplicationLogMessage[]>();
                    }, token);
            }
        }

        [Obsolete]
        public async Task<dynamic> GetWorkOrderApplicationLogs(string id, CancellationToken token)
        {
            return null;

            //using (var client = new AuditableHttpClient(logger))
            //{
            //  client.BaseAddress = new Uri(SimaticService.GetUrl());

            //  // We want the response to be JSON.
            //  client.DefaultRequestHeaders.Accept.Clear();
            //  client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //  // Add the Authorization header with the AccessToken.
            //  client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

            //  var url = $"sit-svc/application/WebCoreApplication/odata/ApplicationLog?$filter=CorrelationId eq {id}";

            //  HttpResponseMessage response = await client.GetAsync(url, token);

            //  SimaticServerHelper.CheckFaultResponse(token, response, logger);

            //  return await response.Content.ReadAsStringAsync()
            //      .ContinueWith(task =>
            //      {
            //        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            //        return result.value.ToObject<ApplicationLog[]>();
            //      }, token);
            //}
        }

        public async Task<dynamic> GetWorkOrderExtendedByOrderNIdAsync(string id, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/WorkOrderExtended?$filter=WorkOrder_Id eq {id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<WorkOrderExtended>)result.value.ToObject<WorkOrderExtended[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = "Work Order extended not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderExtendedByWorkOrderIdAsync(Guid id, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/PICore/odata/WorkOrderExtended?$expand=WorkOrder&$filter=WorkOrder_Id eq {id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<WorkOrderExtended>)result.value.ToObject<WorkOrderExtended[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = "Work Order extended not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderByIdAsync(string id, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/WorkOrder?$filter=Id eq {id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<WorkOrder>)result.value.ToObject<WorkOrder[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = "Order not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<string> UpdateWorkOrderExtendedV2(string id, DateTime startTime, DateTime endTime, decimal quantity, string unitOfMeasure, CancellationToken ct)
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
                        Id = id,
                        PlannedStartTime = startTime,
                        PlannedEndTime = endTime,
                        Quantity = new { UoMNId = UomService.GetSimaticUOM(unitOfMeasure), QuantityValue = quantity }
                    }
                });

                var response = await client.PostAsync("sit-svc/application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> UpdateWorkOrderOperationExtended(string id, string actualEquipmentNId, string description, bool isInExecutionPropagation, string name, string operationNId, string operationRevision, bool resetProperty, int sequence, CancellationToken ct)
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
                        ActualEquipmentNId = actualEquipmentNId,
                        Description = description,
                        Id = id,
                        IsInExecutionPropagation = isInExecutionPropagation,
                        Name = name,
                        OperationNId = operationNId,
                        OperationRevision = operationRevision,
                        ResetProperty = resetProperty,
                        Sequence = sequence
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderOperationExtended",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> GetOrderByNameAsync(string name, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/Order?$filter=Name eq '{name}'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<Order>)result.value.ToObject<Order[]>()).First();

                      dynamic errorMessage = new { Error = $"Order [{name}] not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<string> UpdateWorkOrderOperationEquipmentRequirement(string id, string equipmentGroupNId, string equipmentNId, bool isMain, string plannedEquimentNId, string requierementTag, int sequence, CancellationToken ct)
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
                        EquipmentGroupNId = equipmentGroupNId,
                        EquipmentNId = equipmentNId,
                        Id = id,
                        IsMain = isMain,
                        PlannedEquimentNId = plannedEquimentNId,
                        RequierementTag = requierementTag,
                        Sequence = sequence
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderOperationEquipmentRequirement",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> GetOrderByNIdAsync(string id, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/Order?$filter=NId eq '{id}'&$expand=Operations";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return ((IList<Order>)result.value.ToObject<Order[]>()).First();

                      if (!throwException)
                          return null;

                      dynamic errorMessage = new { Error = $"Order [{id}] not found." };
                      throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderByNIdAsync(string nid, bool throwException, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/WorkOrder?$filter=NId eq '{nid}'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return ((IList<WorkOrder>)result.value.ToObject<WorkOrder[]>()).First();

                        if (!throwException)
                            return null;

                        dynamic errorMessage = new { Error = "Work Order not found." };
                        throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                    }, token);
            }
        }

        /// <summary>
        /// Returns all WorkOrder Operations.
        /// </summary>
        /// <param name="WorkOrderId"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<dynamic> GetWorkOrderOperationAsync(Guid WorkOrderId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/WorkOrderOperation?$filter=WorkOrder_Id eq {WorkOrderId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return (IList<WorkOrderOperation>)result.value.ToObject<WorkOrderOperation[]>();

                        dynamic errorMessage = new { Error = "Work Order not found." };
                        throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                    }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationBySequenceAsync(string secuence,string WOId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/WorkOrderOperation?$filter=Sequence eq {secuence}and WorkOrder_Id eq {WOId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return ((IList<WorkOrderOperation>)result.value.ToObject<WorkOrderOperation[]>()).First();

                        dynamic errorMessage = new { Error = "Work Order not found." };
                        throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                    }, token);
            }
        }

        public async Task<dynamic> GetOrderOperationAsync(Guid WorkOrderId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PICore/odata/OrderOperation?$filter=Order_Id eq {WorkOrderId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                    .ContinueWith(task =>
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                        if (result.value.Count >= 1)
                            return (IList<WorkOrderOperation>)result.value.ToObject<WorkOrderOperation[]>();

                        dynamic errorMessage = new { Error = "Work Order not found." };
                        throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
                    }, token);
            }
        }

        public async Task<dynamic> GetOrderUserFields(string OrderId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/PIOrder/odata/OrderUserField?$filter=Order_Id eq {OrderId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<OrderUserField>)result.value.ToObject<OrderUserField[]>();

                      return new List<OrderUserField>();
                  }, token);
            }
        }

        public async Task CreateOrderUserField(string id, string key, string value, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        OrderUserFields = new[] { new { NId = key, UserFieldType = "String", UserFieldValue = value } }
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIOrder/odata/AddOrderUserFieldsToOrder",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateOrderUserField(string id, string value, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        UserFieldValue = value
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIOrder/odata/UpdateOrderUserField",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetOrderStatusAsync(string id, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        OrderId = id,
                        Verb = verb
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIOrder/odata/SetOrderStatus",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetWorkOrderStatusAsync(string id, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Verb = verb
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/UDM/odata/SetWorkOrderStatus",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetWorkOrderActualTimeStart(string id, DateTime? time, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                DateTime? endTime = null;

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        ActualEndTime = endTime,
                        ActualStartTime = time
                    }
                });
                logger.LogInformation($"Json de envio: [{json}]");
                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetWorkOrderActualTimeEnd(string id, DateTime? time, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));
                logger.LogInformation($"Cambio de tiempo al finalizar [{time}]");

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        ActualEndTime = time
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task ClearWorkOrderActualTime(string id, DateTime plannedInit, DateTime plannedFinish, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                DateTime? endTime = null;
                DateTime? startTime = null;
                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        ActualEndTime = endTime,
                        ActualStartTime = startTime,
                        PlannedStartTime = plannedInit,
                        PlannedEndTime = plannedFinish,
                        ResetProperty = true
                    }
                });
                logger.LogInformation($"Json de envio: [{json}]");
                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> GetWorkOrderOperationRevision(string woId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo las Operaciones de Revisión http://arcloud-opc/sit-svc/Application/PICore/odata/WorkOrderOperation?$filter=OperationNId eq 'REVISION' and WorkOrder_Id eq 

                var url = $"sit-svc/Application/PICore/odata/WorkOrderOperation?$filter=OperationNId eq 'REVISION' and WorkOrder_Id eq {woId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderOperation>)result.value.ToObject<WorkOrderOperation[]>();

                      return new List<WorkOrderOperation>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationParameterSpecificationRollos(string woId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo los Parametros de Revisión que traen lo que corresponden a Rollos http://arcloud-opc/sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=WorkOrderOperation_Id eq d85b104c-57de-ee11-b83b-020017035491 and startswith(ParameterNId, 'Rollo')

                var url = $"sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=WorkOrderOperation_Id eq {woId} and startswith(ParameterNId, 'Rollo')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderOperationParameterSpecification>)result.value.ToObject<WorkOrderOperationParameterSpecification[]>();

                      return new List<WorkOrderOperationParameterSpecification>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationMaterialRequirementAndMaterial(string Id, string MaterialNId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo los Materiales de Empaque http://arcloud-opc/sit-svc/Application/PICore/odata/RF_GetWorkOrderOperationMaterialRequirementAndMaterialInfos(function=@x)?@x={}&$filter=WorkOrderOperationId eq f941de86-470d-ef11-b841-020017035491

                var url = $"sit-svc/Application/PICore/odata/RF_GetWorkOrderOperationMaterialRequirementAndMaterialInfos(function=@x)?@x={{}}&$filter=WorkOrderOperationId eq {Id} and startswith(MaterialNId, '{MaterialNId}')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<MaterialResponse>)result.value.ToObject<MaterialResponse[]>();

                      return new List<MaterialResponse>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationParameterSpecificationRollosWithOutDeclarar(string woId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo los Parametros de Revisión que traen lo que corresponden a Rollos http://arcloud-opc/sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=WorkOrderOperation_Id eq d85b104c-57de-ee11-b83b-020017035491 and startswith(ParameterNId, 'Rollo')

                var url = $"sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=WorkOrderOperation_Id eq {woId} and startswith(ParameterNId, 'Rollo') and (ParameterLimitLow eq '0' or ParameterLimitLow eq null or ParameterLimitLow eq '')";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderOperationParameterSpecification>)result.value.ToObject<WorkOrderOperationParameterSpecification[]>();

                      return new List<WorkOrderOperationParameterSpecification>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationParameterSpecificationId(string Id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo los Parametros de Revisión que traen lo que corresponden a Rollos http://arcloud-opc/sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=WorkOrderOperation_Id eq d85b104c-57de-ee11-b83b-020017035491 and startswith(ParameterNId, 'Rollo')

                var url = $"sit-svc/Application/PICore/odata/WorkOrderOperationParameterSpecification?$filter=Id eq {Id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderOperationParameterSpecification>)result.value.ToObject<WorkOrderOperationParameterSpecification[]>();

                      return new List<WorkOrderOperationParameterSpecification>();
                  }, token);
            }
        }

        public async Task<IList<MaterialTrackingUnitProperty>> GetMaterialTrackingUnitProperty(string NId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo los Parametros de la MTU que traen lo que corresponden a cada Revision con su MTU http://arcloud-opc/sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '000043.05K07eA1000'

                var url = $"sit-svc/Application/Material/odata/MaterialTrackingUnitProperty?$filter=MaterialTrackingUnit/NId eq '{NId}'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<MaterialTrackingUnitProperty>)result.value.ToObject<MaterialTrackingUnitProperty[]>();

                      return new List<MaterialTrackingUnitProperty>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderOperationAny(string woId, string OperationNId, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // URL para extraer solo las Operaciones de Revisión http://arcloud-opc/sit-svc/Application/PICore/odata/WorkOrderOperation?$filter=OperationNId eq 'REVISION' and WorkOrder_Id eq 

                var url = $"sit-svc/Application/PICore/odata/WorkOrderOperation?$filter=OperationNId eq '{OperationNId}' and WorkOrder_Id eq {woId}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderOperation>)result.value.ToObject<WorkOrderOperation[]>();

                      return new List<WorkOrderOperation>();
                  }, token);
            }
        }

        public async Task SetWorkOrderOperationAsync(WorkOrderOperationRevisionRequest prod, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {

                    //{
                    //    "Id": "c63d8483-34c1-ee11-b835-020017035491",
                    //    "WorkOrderOperations": [
                    //        {
                    //            "NId": "5829",
                    //            "Name": "Prueba",
                    //            "Description": "Descripcion Prueba",
                    //            "Sequence": 41,
                    //            "OperationNId": "CORTE",
                    //            "OperationRevision": "1"
                    //        }
                    //    ]
                    //}

                    command = new
                    {
                        Id = prod.Id,
                        WorkOrderOperations = prod.ProcessParameters
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                //var response = await client.PostAsync("http://arcloud-opc/sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                //  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task SetWorkOrderOperationParametersSpecificationAsync(WorkOrderOperationRevisionRollosRequest prod, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {

                    //{
                    //"command": {
                    //    "Id": "d85b104c-57de-ee11-b83b-020017035491",
                    //        "ProcessParameters": [
                    //            {
                    //                "ParameterNId": "Rollo_2",
                    //                "ParameterTargetValue": "TV",
                    //                "ParameterLimitLow": null,
                    //                "ParameterToleranceLow": "LT",
                    //                "ParameterToleranceHigh": null,
                    //                "ParameterLimitHigh": null,
                    //                "IsLimitsInPercentage": false,
                    //                "ParameterUoMNId": null
                    //            },
                    //            {
                    //                "ParameterNId": "Rollo_3",
                    //                "ParameterTargetValue": "TV",
                    //                "ParameterLimitLow": null,
                    //                "ParameterToleranceLow": "LT",
                    //                "ParameterToleranceHigh": null,
                    //                "ParameterLimitHigh": null,
                    //                "IsLimitsInPercentage": false,
                    //                "ParameterUoMNId": null
                    //            }
                    //        ]
                    //    }
                    //}

                    command = new
                    {
                        Id = prod.Id,
                        ProcessParameters = prod.ProcessParameters
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/AddParameterRequirementsToWorkOrderOperationFromCatalogCalculatingPercentages",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                //var response = await client.PostAsync("http://arcloud-opc/sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                //  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateWorkOrderOperationParameterRequirementAsync(IList<UpdateParameterSpecification> prod, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {

                    //{
                    //"command": {
                    //    "Id": "d85b104c-57de-ee11-b83b-020017035491",
                    //        "ProcessParameters": [
                    //            {
                    //                "ParameterNId": "Rollo_2",
                    //                "ParameterTargetValue": "TV",
                    //                "ParameterLimitLow": null,
                    //                "ParameterToleranceLow": "LT",
                    //                "ParameterToleranceHigh": null,
                    //                "ParameterLimitHigh": null,
                    //                "IsLimitsInPercentage": false,
                    //                "ParameterUoMNId": null
                    //            },
                    //            {
                    //                "ParameterNId": "Rollo_3",
                    //                "ParameterTargetValue": "TV",
                    //                "ParameterLimitLow": null,
                    //                "ParameterToleranceLow": "LT",
                    //                "ParameterToleranceHigh": null,
                    //                "ParameterLimitHigh": null,
                    //                "IsLimitsInPercentage": false,
                    //                "ParameterUoMNId": null
                    //            }
                    //        ]
                    //    }
                    //}

                    command = new
                    {
                        Parameters = prod
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderOperationParameterRequirement",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                //var response = await client.PostAsync("http://arcloud-opc/sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                //  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task AddWorkOrderOperationMaterialRequirementsToWorkOrderOperation(IList<MaterialRequirement> prod, string workOrderOperationId, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        WorkOrderOperationId = workOrderOperationId,
                        WorkOrderOperationMaterialRequirements = prod
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/AddWorkOrderOperationMaterialRequirementsToWorkOrderOperation",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                //var response = await client.PostAsync("http://arcloud-opc/sit-svc/Application/PICore/odata/UpdateWorkOrderExtendedV2",
                //  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task UpdateCounterTrimCorteAsync(UpdateParameterSpecification prod, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {

                    //{
                    //"command": {
                    //    "Id": "d85b104c-57de-ee11-b83b-020017035491",
                    //        "Parameters": [
                    //            {
                    //                "Id": "8cd233f9-2cf9-ee11-b83f-020017035491",
                    //                "ParameterValue": "60RX7.5 POR 1000MTS = 450.00 CM 3C ",
                    //                "ParameterLimitLow": null,
                    //                "ParameterToleranceLow": "1",
                    //                "ParameterToleranceHigh": null,
                    //                "ParameterLimitHigh": null,
                    //                "ParameterActualValue": null,
                    //                "EquipmentNId": "",
                    //                "ParameterNId": "Trim_1",
                    //                "WorkProcessVariableNId": null,
                    //                "TaskParameterNId": null
                    //            }
                    //            
                    //        ]
                    //    }
                    //}

                    command = new
                    {
                        Parameters = new[] { prod }
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateWorkOrderOperationParameterRequirement",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Sets WorkOrder Operation status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="verb"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task SetWorkOrderOperationStatusAsync(string id, string verb, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        Verb = verb
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/UDM/odata/SetWorkOrderOperationStatus",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task DeleteOrderAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIOrder/odata/DeleteOrder",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Deletes ORder Operation by Id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task DeleteOrderOperationAsync(Guid id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Ids = new List<Guid>() { id }
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PIOrder/odata/DeleteOrderOperations",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task DeleteWorkOrderAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/UDM/odata/DeleteWorkOrder",
                    new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        #region WO parameters
        public async Task<dynamic> GetWorkOrderUserFieldsByID(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/application/UDM/odata/WorkOrderUserField?$filter=WorkOrder_Id eq {id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<WorkOrderUserField>)result.value.ToObject<WorkOrderUserField[]>();

                      return new List<WorkOrderUserField>();
                  }, token);
            }
        }

        public async Task UpdateWorkOrderUserField(string id, string value, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id,
                        UserFieldValue = value
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/UDM/odata/UpdateWorkOrderUserField",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> GetWorkOrderParametersAsync(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/PICore/odata/WorkOrderHeaderParameter?$skip=0&$orderby=ParameterNId%20asc&$count=true&$filter=WorkOrder_Id eq {id}";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

                      if (result.value.Count >= 1)
                          return (IList<ProcessParameter>)result.value.ToObject<ProcessParameter[]>();

                      return new List<ProcessParameter>();
                  }, token);
            }
        }

        public async Task<dynamic> GetWorkOrderParameterUltimaDeclarion(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                var url = $"sit-svc/Application/PICore/odata/WorkOrderHeaderParameter?$count=true&$filter=WorkOrder_Id eq {id} and ParameterNId eq 'ultimaDeclaracion'";

                HttpResponseMessage response = await client.GetAsync(url, token);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      logger.LogInformation($"parameter {result}");

                      if (result.value.Count >= 1)
                      {

                          return (IList<ProcessParameter>)result.value.ToObject<ProcessParameter[]>();

                      }
                      else
                      {
                          return (IList<ProcessParameter>)result.value.ToObject<ProcessParameter[]>();


                      }
                  }, token);
            }
        }

        public async Task AddWorkOrderParametersAsync(string woId, Params[] parameters, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = woId,
                        ProcessParameters = parameters

                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/AddProcessParametersToWorkOrderHeader",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<dynamic> UpdateWorkOrderParametersAsync(string propId, string propValue, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = propId,
                        ParameterValue = propValue,

                    }

                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/UpdateProcessParameterToWorkOrderHeader",
                  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task DeleteWorkOrderParameter(string id, CancellationToken token)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri(SimaticService.GetUrl());

                // We want the response to be JSON.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Add the Authorization header with the AccessToken.
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

                // Build up the data to POST.
                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        Id = id
                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/DeleteProcessParameterToWorkOrderHeader",
                    new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(token, response, logger);

                await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        /// <summary>
        /// Creates a WorkOrder from Order / by WorkMaster.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="material"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Guid>> CreateWorkOrderAsync(Order order, Material material, CancellationToken ct)
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
                //        Request URL: http://localhost/sit-svc/Application/PICore/odata/CreateWorkOrders

                //          With WorkMaster
                //{ "command":{ "Quantity":{ "QuantityValue":999,"UoMNId":"m2"},"WorkMasterNId":"2LPBBU.004","WorkMasterRevision":"1","Priority":1,"NumberOfWorkOrdersToBeCreated":1,"PlannedStartTime":null,"PlannedEndTime":null,"OrderNId":"22045","TemplateNId":"Normal","MaterialNId":"2LPBBU.042"} }

                //          Only Order
                //{ "command":{ "Quantity":{ "QuantityValue":990,"UoMNId":"m2"},"Priority":1,"NumberOfWorkOrdersToBeCreated":1,"PlannedStartTime":null,"PlannedEndTime":null,"OrderNId":"22045","TemplateNId":"Normal","MaterialNId":"2LPBBU.004"} }

                var json = JsonConvert.SerializeObject(new
                {
                    command = new
                    {
                        OrderNId = order.NId,
                        TemplateNId = order.Type,
                        Priority = 1,
                        NumberOfWorkOrdersToBeCreated = 1,
                        MaterialNId = order.MaterialNId,
                        Quantity = new { QuantityValue = order.Quantity.QuantityValue, UoMNId = order.Quantity.UoMNId },
                        PlannedStartTime = order.PlannedStartTime,
                        PlannedEndTime = order.PlannedEndTime,

                    }
                });

                var response = await client.PostAsync("sit-svc/Application/PICore/odata/CreateWorkOrders",
                  new StringContent(json, Encoding.UTF8, "application/json"), ct).ConfigureAwait(true);

                SimaticServerHelper.CheckFaultResponse(ct, response, logger);

                //if (result.value.Count >= 1)
                //  return ((IList<BillOfMaterials>)result.value.ToObject<BillOfMaterials[]>()).First();


                return await response.Content.ReadAsStringAsync()
                  .ContinueWith(task =>
                  {
                      var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
                      return result.Ids.ToObject<List<Guid>>();
                  }, ct);
            }
        }

        public Task<string> UpdateWorkOrderParametersRollosClientes(string ordenVenta, string Ancho, string Cliente, string NroRollos, string M2, string Notas, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpResponseMessage> PostToRTDSWriteAsync(RTDS requestBody, CancellationToken ct)
        {
            using (var client = new AuditableHttpClient(logger))
            {
                client.BaseAddress = new Uri("http://10.10.0.134:9000");

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));



                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/RTDS/RTDSWrite", content, ct).ConfigureAwait(false);
                logger.LogInformation($"RDTS RESPONSE:[{response}]");


                return response;
            }
        }
        #endregion

    }
}

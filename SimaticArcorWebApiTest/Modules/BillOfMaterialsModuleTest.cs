using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Moq;
using Nancy.Extensions;
using Nancy.IO;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using NUnit.Framework;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Modules;
using Nancy.Testing;
using Newtonsoft.Json;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;

namespace SimaticArcorWebApiTest.Modules
{
	public class BillOfMaterialsModuleTest
	{
		public BillOfMaterialsModule Module { get; set; }

		public Mock<IBOMService> MockBOMService { get; set; }

		public Mock<ISimaticBOMService> MockSimaticBOMService { get; set; }

		[SetUp]
		public void Init()
		{
			ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;

			IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()
				{
					{ "NancyConfig:enableRequestLogging", "true" }
				})
				.Build();

			MockBOMService = new Mock<IBOMService>();

			MockSimaticBOMService = new Mock<ISimaticBOMService>();
			Module = new BillOfMaterialsModule(configuration, MockBOMService.Object, MockSimaticBOMService.Object);
		}

		[Test]
		public async Task T01_CreateBillOfMaterialsRequestValidationTest()
		{
		
			var request = this.BOMRequest;

			var browser = new Browser(with => with.Module(Module));
			
			var response = await browser.Post("api/BillOfMaterials/", (with) =>
			{
				with.HttpRequest();
				with.Header("Accept", "application/json");
				with.JsonBody(request);
			});
			// request
			MockBOMService.Verify(i => i.ProcessNewBomAsync(It.IsAny<BillOfMaterialsRequest>(), It.IsAny<CancellationToken>()));

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "Error al procesar el mensaje");
		
		}

		[Test]
		public async Task T02_CreateBillOfMaterialsRequestInvalidJsonTest()
		{
			{
				var template = this.BOMRequest;

				//ID validation
				var request = template;
				request.Id = null;
				var browser = new Browser(with => with.Module(Module));

				var response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo ID del request. Response: [{response.Body}]");

				//Plant Validation
				request = template;
				request.Plant = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Plant del request. Response: [{response.Body.AsString()}]");

				//Quantity Validation
				request = template;
				request.QuantityValue = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Quantity del request. Response: [{response.Body.AsString()}]");

				//List Validation
				request = template;
				request.List = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo List del request. Response: [{response.Body.AsString()}]");

				//UoMNId Validation
				request = template;
				request.UoMNId = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo UoMNId del request. Response: [{response.Body.AsString()}]");

				//Items Validation
				request = template;
				template.Items = template.Items.ToArray();
				request.Items = new BillOfMaterialsRequestItem[0];
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Items del request. Response: [{ response.Body.AsString()}]");
				
				MockBOMService.Verify(i => i.ProcessNewBomAsync(It.IsAny<BillOfMaterialsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
				
			}

		}
		
		[Test]
		public async Task T03_CreateBillOfMaterialsBulkRequestValidationTest()
		{
			var request = new List<BillOfMaterialsRequest>();
			request.Add(this.BOMRequest);
			request.Add(this.BOMRequest);
			request.Add(this.BOMRequest);
			request.Add(this.BOMRequest);

			var browser = new Browser(with => with.Module(Module));

			var response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
			{
				with.HttpRequest();
				with.Header("Accept", "application/json");
				with.JsonBody(request);
			});
			// request
			MockBOMService.Verify(i => i.ProcessNewBomAsync(It.IsAny<BillOfMaterialsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(4));

			Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "Error al procesar el mensaje");
			
		}

		[Test]
		public async Task T04_CreateBillOfMaterialsBulkRequestInvalidJsonTest()
		{
			{
				var template = new List<BillOfMaterialsRequest>();
				template.Add(this.BOMRequest);

				//ID validation
				var request = template.ToList();

				request.First().Id = null;
				var browser = new Browser(with => with.Module(Module));

				var response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo ID del request. Response: [{response.Body}]");

				//Plant Validation
				request = template.ToList();
				request.First().Plant = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Plant del request. Response: [{response.Body.AsString()}]");

				//Quantity Validation
				request = template.ToList();
				request.First().QuantityValue = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Quantity del request. Response: [{response.Body.AsString()}]");

				//List Validation
				request = template.ToList();
				request.First().List = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo List del request. Response: [{response.Body.AsString()}]");

				//UoMNId Validation
				request = template.ToList();
				request.First().UoMNId = null;
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo UoMNId del request. Response: [{response.Body.AsString()}]");

				//Items Validation
				request = template.ToList();
				request.First().Items = new BillOfMaterialsRequestItem[0];
				browser = new Browser(with => with.Module(Module));

				response = await browser.Post("api/BillOfMaterials/bulkInsert", (with) =>
				{
					with.HttpRequest();
					with.Header("Accept", "application/json");
					with.JsonBody(request);
				});
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"Error El Json debería haber validado el campo Items del request. Response: [{ response.Body.AsString()}]");

				MockBOMService.Verify(i => i.ProcessNewBomAsync(It.IsAny<BillOfMaterialsRequest>(), It.IsAny<CancellationToken>()), Times.Never);

			}

		}

		#region Helper

		public BillOfMaterialsRequest BOMRequest =>
			new BillOfMaterialsRequest()
			{
				Id = "781217",
				Plant = "1001",
				QuantityValue = 1,
				UoMNId = "UN",
				List = "M",
				Items = (new List<BillOfMaterialsRequestItem>
				{
					new BillOfMaterialsRequestItem()
					{
						MaterialId = "781200",
						QuantityValue = 6,
						UoMNId = "UN",
						Sequence = 1,
						Scrap = 0,
						From = Convert.ToDateTime("2019-08-30T00:00:00"),
						To = Convert.ToDateTime("2040-12-31T00:00:00")
					},
					new BillOfMaterialsRequestItem()
					{
						MaterialId = "3488",
						QuantityValue = 0.00926,
						UoMNId = "KG",
						Sequence = 1,
						Scrap = 0.0000926,
						From = Convert.ToDateTime("2019-08-30T00:00:00"),
						To = Convert.ToDateTime("2040-12-31T00:00:00")
					}
				}).ToArray()
			};

		#endregion
	}
}
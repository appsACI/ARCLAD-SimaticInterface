using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy;
using Moq;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using NUnit.Framework;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Modules;
using Nancy.Testing;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApiTest.Modules
{
  public class OrderModuleTest
  {
    public OrderModule Module { get; set; }
    
    public Mock<IOrderService> MockOrderService { get; set; }

    public Mock<ISimaticOrderService> MockSimaticOrderService { get; set; }

    [SetUp]
    public void Init()
    {
      ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;

      IConfiguration configuration = new ConfigurationBuilder().Build();

      MockOrderService = new Mock<IOrderService>();
      
      MockSimaticOrderService = new Mock<ISimaticOrderService>();
      Module = new OrderModule(configuration, MockOrderService.Object, MockSimaticOrderService.Object);
    }

    [Test]
    public async Task T01_CreateOrderRequestValidationTest()
    {
      var request = new ProductionRequest()
      {
        Id = "ID1131",
        StartTime = DateTime.Now,
        EndTime = DateTime.Now.AddDays(1),
        FinalMaterial = "MAT01",
        Status = "43",
        ExportNbr = "Number 1234",
        Quantity = 100,
        FamilyID = "WO",
        OrderId = "10011101",
        Line = "Linea 1",
        PlantID = "1001",
        ShipmentOrder = "OR1010111",
        UnitOfMeasure = "BU"
      };

      MockOrderService.Setup(i => i.ProcessNewOrderAsync(request, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

      var browser = new Browser(with => with.Module(Module));

      var response = await browser.Post("api/orders/", (with) =>
      {
        with.HttpRequest();
        with.Header("Accept", "application/json");
        with.JsonBody(request);
      });

      // request
      MockOrderService.Verify(i => i.ProcessNewOrderAsync(It.IsAny<ProductionRequest>(), It.IsAny<CancellationToken>()));

      Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "Error al procesar el mensaje");
    }


  }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Moq;
using Moq.Protected;
using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Management;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticArcorWebApi.Modules;
using SimaticArcorWebApiTest.Helper;

namespace SimaticArcorWebApiTest.Management
{
  public class OrderServiceTest
	{
    public OrderService Service { get; set; }
    
    public Mock<ISimaticOrderService> MockSimaticOrderService { get; set; }

    public Mock<ISimaticMaterialService> MockSimaticMaterialService { get; set; }

    [SetUp]
    public void Init()
    {
      ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;

      MockSimaticOrderService = new Mock<ISimaticOrderService>();
      MockSimaticMaterialService = new Mock<ISimaticMaterialService>();
			
      Service = new OrderService(MockSimaticOrderService.Object, MockSimaticMaterialService.Object);
    }

		/// <summary>
		/// Test new order without userfields
		/// </summary>
		/// <returns></returns>
    [Test]
    public async Task T01_ProcessNewOrderAsyncTest()
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
			
	    MockSimaticMaterialService.Setup(x => x.GetMaterialByNIdAsync(It.Is<string>(s => s == request.FinalMaterial),
		    It.IsAny<bool>(),
		    It.IsAny<bool>(),
		    It.IsAny<CancellationToken>())).ReturnsAsync(new Material()
	    {
					Id = "asdfsadasdasdasdas",
					NId = request.FinalMaterial,
					Name = request.FinalMaterial,
	    });

	    var orderId = "1313131sfd";
	    var woId = "14321sdofijsd";
			
			MockSimaticOrderService.Setup(x => x.CreateOrderAsync(
				It.Is<string>(s => s == request.OrderId),
				It.Is<string>(s => s == request.Id),
				It.Is<DateTime>(s => s == request.StartTime),
				It.Is<DateTime>(s => s == request.EndTime),
				It.Is<string>(s => s == request.FamilyID),
				It.Is<string>(s => s == request.Status),
				It.Is<string>(s => s == request.FinalMaterial),
				It.Is<decimal>(s => s == request.Quantity),
				It.Is<string>(s => s == request.UnitOfMeasure),
				It.IsAny<CancellationToken>())).ReturnsAsync(orderId);

			MockSimaticOrderService.Setup(x => x.GetOrderUserFields(
				It.Is<string>(s => s == orderId),
				It.IsAny<CancellationToken>())).ReturnsAsync(new List<OrderUserField>());

			MockSimaticOrderService.Setup(x => x.CreateOrderUserField(
				It.Is<string>(s => s == request.OrderId),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()));
			
			MockSimaticOrderService.Setup(x => x.CallCreateOrderFromOperationCommand(
				It.Is<string>(s => s == request.OrderId),
                It.Is<string>(s => s == request.Status),
                It.IsAny<CancellationToken>())).ReturnsAsync(woId);

			MockSimaticOrderService.Setup(x => x.GetWorkOrderUserFieldsByID(
				It.Is<string>(s => s == woId),
				It.IsAny<CancellationToken>())).ReturnsAsync(new List<WorkOrderUserField>());

			MockSimaticOrderService.Setup(x => x.SetOrderStatus(
				It.Is<string>(s => s == woId),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()));

			await Service.ProcessNewOrderAsync(request, new CancellationToken());
    }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[Test]
		public async Task T02_ProcessNewMaterialNotFoundExceptionTest()
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

			MockSimaticMaterialService.Setup(x => x.GetMaterialByNIdAsync(It.IsAny<string>(),
				It.IsAny<bool>(),
				It.IsAny<bool>(),
				It.IsAny<CancellationToken>())).ReturnsAsync((Material)null);
			
			Assert.Throws<Exception>(() => Service.ProcessNewOrderAsync(request, new CancellationToken()).GetAwaiter().GetResult());
		}


	}
}

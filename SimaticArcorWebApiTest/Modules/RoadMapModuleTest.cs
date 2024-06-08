using System;
using System.Collections.Generic;
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
using SimaticArcorWebApi.Model.Custom.RoadMap;

namespace SimaticArcorWebApiTest.Modules
{
  public class SimaticRoadMapServiceTest
  {
    public RoadMapModule Module { get; set; }
    
    public Mock<IMaterialService> MockMaterialService { get; set; }

    public Mock<ISimaticRoadMapService> MockSimaticRoadMapService { get; set; }

    [SetUp]
    public void Init()
    {
      ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;

      IConfiguration configuration = new ConfigurationBuilder().Build();

      MockMaterialService = new Mock<IMaterialService>();
      MockSimaticRoadMapService = new Mock<ISimaticRoadMapService>();

      Module = new RoadMapModule(configuration, MockMaterialService.Object, MockSimaticRoadMapService.Object);
    }

    [Test]
    public async Task T01_CreateRoadMapTest()
    {
      /*
      {
        "id": "90101004942",
        "plant": "Salto",
        "type": "M",
        "quantityValue": "1",
        "uoMNId": "kg",
        "operations": [
        {
          "id": "90101005697",
          "level:": "6",
          "operation": "ENVASADO TERCIARIO",
          "KgHs":40,
          "KgHsMach":45,
          "theoretical": 1926,
          "efficiency": 84.2,
          "crew": "5",
          "costs": 34,
          "machineHs": 5,
          "manHs": 7,
          "from":"2006-05-29T13:13:01",
          "to":"2006-09-19T06:25:30"

        }
        ]
      }
      */
      var request = new RoadMapRequest()
      {
        Id = "90101004942",
        Plant = "Salto",
        Type = "M",
        QuantityValue = 1,
        UoMNId = "kg",
        Operations = new List<RoadMapRequestOperations> { 
            new RoadMapRequestOperations {
              Id = "90101005697",
              Level = "6",
              Operation = "ENVASADO TERCIARIO",
              KgHs = 40,
              KgHsMach = 45,
              Theoretical = 1926,
              Efficiency = 84.2,
              Crew = 5,
              Costs = 34,
              MachineHs = 5,
              ManHs = 7,
              From = Convert.ToDateTime("2006-05-29T13:13:01"),
              To = Convert.ToDateTime("2006-09-19T06:25:30")
            }
          }.ToArray()
      };

      MockSimaticRoadMapService.Setup(i => i.CreateRoadMapAsync(request.Id, request.Plant, request.Type, request.QuantityValue, request.UoMNId, It.IsAny<RoadMapRequestOperations[]>(), It.IsAny<CancellationToken>()))
        .Returns(Task.FromResult<string>(string.Empty));

      var browser = new Browser(with => with.Module(Module));

      var response = await browser.Post("api/roadmaps/", (with) =>
      {
        with.HttpRequest();
        with.Header("Accept", "application/json");
        with.JsonBody(request);
      });

      // request
      MockSimaticRoadMapService.Verify(i => i.CreateRoadMapAsync(request.Id, request.Plant, request.Type,
        request.QuantityValue, request.UoMNId, It.IsAny<RoadMapRequestOperations[]>(), It.IsAny<CancellationToken>()));

      Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "Error al procesar el mensaje");
    }


  }
}

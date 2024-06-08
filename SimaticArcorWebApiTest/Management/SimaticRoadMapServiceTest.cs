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
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Modules;
using SimaticArcorWebApiTest.Helper;

namespace SimaticArcorWebApiTest.Management
{
  public class SimaticRoadMapServiceTest
  {
    public SimaticRoadMapService Service { get; set; }
    
    public Mock<ISimaticService> MockSimaticService { get; set; }

    public Mock<IUOMService> MockUOMService { get; set; }

    public FakeResponseHandler HttpClientHandler { get; set; }

    private string url = "http://FAKE_URL.COM/";

    [SetUp]
    public void Init()
    {
      ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;
      
      MockSimaticService = new Mock<ISimaticService>();
      MockUOMService = new Mock<IUOMService>();


      MockSimaticService.Setup(i => i.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("TOKEN1231431312");
      MockSimaticService.Setup(i => i.GetUrl()).Returns(url);

      //MockAuditableHttpClient = new Mock<AuditableHttpClient>();

      Service = new SimaticRoadMapService(MockSimaticService.Object, MockUOMService.Object);

      HttpClientHandler = new FakeResponseHandler();
      Service.HttpClientHandler = HttpClientHandler;

    }

    [Test]
    public async Task T01_InsertRoadMapTest()
    {
      string guid = "141414123121341";
      string id = "90101004942";
      string plant = "Salto";
      string type = "M";
      decimal quantityValue = 1;
      string uoMNId = "kg";
      var operations = new List<RoadMapRequestOperations>
      {
        new RoadMapRequestOperations
        {
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
      }.ToArray();

      HttpClientHandler.AddFakeResponse(new Uri(url + "sit-svc/application/PICore/odata/ArcorPICore_CreateRoadMap"), new HttpResponseMessage(System.Net.HttpStatusCode.OK)
      {
        Content = new StringContent
        (@"{
						'Id':" + guid + @"
					}")
      });
      
      var rmId = await Service.CreateRoadMapAsync(id, plant, type, quantityValue, uoMNId, operations, new CancellationToken());

      Assert.AreEqual(rmId, guid, "Error al crear la hoja de ruta");
    }


  }
}

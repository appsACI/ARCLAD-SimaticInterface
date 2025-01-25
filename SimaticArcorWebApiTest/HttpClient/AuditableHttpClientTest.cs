//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using Endor.Core.Logger;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Debug;
//using Moq;
//using Moq.Protected;
//using Nancy;
//using Nancy.Testing;
//using NUnit.Framework;
//using SimaticArcorWebApi.HttpClient;
//using SimaticArcorWebApi.Management;
//using SimaticArcorWebApi.Model.Custom.RoadMap;
//using SimaticArcorWebApi.Modules;

//namespace SimaticArcorWebApiTest.HttpClient
//{
//  public class AuditableHttpClientTest
//  {
//    public AuditableHttpClient Service { get; set; }
    
//    public Mock<IMaterialService> MockMaterialService { get; set; }

//    public Mock<ISimaticRoadMapService> MockSimaticRoadMapService { get; set; }

//    [SetUp]
//    public void Init()
//    {
//      ApplicationLogging.LoggerFactory = new Mock<LoggerFactory>().Object;

//      var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//      handlerMock
//        .Protected()
//        // Setup the PROTECTED method to mock
//        .Setup<Task<HttpResponseMessage>>(
//          "SendAsync",
//          ItExpr.IsAny<HttpRequestMessage>(),
//          ItExpr.IsAny<CancellationToken>()
//        )
//        // prepare the expected response of the mocked http call
//        .ReturnsAsync(new HttpResponseMessage()
//        {
//          StatusCode = System.Net.HttpStatusCode.OK,
//          Content = new StringContent("[{'id':1,'value':'1'}]"),
//        })
//        .Verifiable();

//      Service = new AuditableHttpClient(new DebugLogger("Test"), handlerMock.Object);
//    }

//    [Test]
//    public async Task T01_AuditableHttpClientSendAsyncTest()
//    {
      
//    }


//  }
//}

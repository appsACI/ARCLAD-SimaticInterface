using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.RoadMap;

namespace SimaticArcorWebApi.Management
{
  public interface ISimaticRoadMapService
  {
    Task<dynamic> GetAllRoadMapOperationsAsync(CancellationToken token);

    Task<dynamic> GetAllRoadMapOperationsByIdAsync(string id, CancellationToken ct);

    Task<dynamic> GetRoadMapByNIdAsync(string nid, CancellationToken ct);

    Task<string> CreateRoadMapAsync(string id, string plant, string type, decimal quantityValue,
      string uoMId, RoadMapRequestOperations[] operations, CancellationToken token);
  }
}

using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
  public interface IBOMService
  {
    Task ProcessNewBomAsync(BillOfMaterialsRequest bom, CancellationToken token);

    Task AddUomConversionEntity(UomConversionRequest uom, CancellationToken token);

  }
}

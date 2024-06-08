using SimaticArcorWebApi.Model.Custom;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
  public interface IPQRLMSService
    {

    Task<PQRLMSResponse> ProcessNewPQRLMSAsync(PQRLMSRequest bom, CancellationToken token);

    }
}
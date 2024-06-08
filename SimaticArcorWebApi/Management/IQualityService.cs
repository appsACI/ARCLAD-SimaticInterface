using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom.Quality;

namespace SimaticArcorWebApi.Management
{
    public interface IQualityService
    {
        Task CreateProductSpecification(CreateProductSpecificationRequest req, CancellationToken ct);
        Task CreateSample(CreateSampleRequest req, CancellationToken ct);
    }
}

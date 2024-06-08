using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApi.Management
{
  public interface IMaterialService
  {
    Task CreateMaterialAsync(MaterialRequest mat, bool disable, CancellationToken token);

    Task ProcessNewMaterialAsync(MaterialRequest mat, CancellationToken token);
  }
}

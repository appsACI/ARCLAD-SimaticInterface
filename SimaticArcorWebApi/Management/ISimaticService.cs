using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
    public interface ISimaticService
    {
        Task<string> GetAccessToken(CancellationToken token, bool force = false);
        string GetUrl();

    }
}
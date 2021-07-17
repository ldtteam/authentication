using System.Threading;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Api
{
    public interface IStartupTask
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
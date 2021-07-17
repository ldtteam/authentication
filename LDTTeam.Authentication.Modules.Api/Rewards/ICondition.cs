using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public interface ICondition
    {
        public string ModuleName { get; }

        public string Name { get; }

        public Task<bool> ExecuteAsync(IServiceScope scope, ConditionInstance instance, string userId, CancellationToken cancellationToken = default);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public class DefaultCondition : ICondition
    {
        public string ModuleName { get; set; }
        
        public string Name { get; set; }

        private readonly Func<IServiceScope, ConditionInstance, string, Task<bool>> _execute;

        public DefaultCondition(string moduleName, string name, Func<IServiceScope, ConditionInstance, string, Task<bool>> execute)
        {
            ModuleName = moduleName;
            Name = name;
            _execute = execute;
        }

        public Task<bool> ExecuteAsync(IServiceScope scope, ConditionInstance instance, string userId, CancellationToken cancellationToken = default)
        {
            return _execute.Invoke(scope, instance, userId);
        }

        public bool Validate(ConditionInstance instance)
        {
            return true;
        }
    }
}
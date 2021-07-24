using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Utils;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace LDTTeam.Authentication.Modules.Api.Events
{
    public class EventsService
    {
        public event Func<Task> TestEvent
        {
            add => _testEvent.Add(value);
            remove => _testEvent.Remove(value);
        }

        public readonly AsyncEvent<Func<Task>> _testEvent = new();
        
        public event Func<Task> ConditionRegistration
        {
            add => _conditionRegistration.Add(value);
            remove => _conditionRegistration.Remove(value);
        }

        public readonly AsyncEvent<Func<Task>> _conditionRegistration = new();
        
        public event Func<IServiceScope, List<string>?, Task> RefreshContentEvent
        {
            add => _refreshContentEvent.Add(value);
            remove => _refreshContentEvent.Remove(value);
        }

        public readonly AsyncEvent<Func<IServiceScope, List<string>?, Task>> _refreshContentEvent = new();
        
        public event Func<IServiceScope, Task> PostRefreshContentEvent
        {
            add => _postRefreshContentEvent.Add(value);
            remove => _postRefreshContentEvent.Remove(value);
        }

        public readonly AsyncEvent<Func<IServiceScope, Task>> _postRefreshContentEvent = new();
    }
}
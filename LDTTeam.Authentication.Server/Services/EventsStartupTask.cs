using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Api.Utils;
using System.Text.Json;

namespace LDTTeam.Authentication.Server.Services
{
    public class EventsStartupTask : IStartupTask
    {
        private readonly IServiceProvider _services;
        private readonly EventsService _eventsService;
        private readonly IBackgroundEventsQueue _eventsQueue;

        public EventsStartupTask(IServiceProvider services, EventsService eventsService, IBackgroundEventsQueue eventsQueue)
        {
            _services = services;
            _eventsService = eventsService;
            _eventsQueue = eventsQueue;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (IModule module in Modules.List)
            {
                module.EventsSubscription(_services, _eventsService, cancellationToken);
            }

            await _eventsService._conditionRegistration.InvokeAsync();

            Console.WriteLine($"Conditions Registered: {JsonSerializer.Serialize(Conditions.Registry)}");

            /*
            await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope, null);
                await events._postRefreshContentEvent.InvokeAsync(scope);
            });*/
        }
    }
}
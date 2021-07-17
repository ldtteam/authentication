using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Server.Services
{
    public class EventsQueueService : BackgroundService
    {
        private readonly ILogger<EventsQueueService> _logger;
        private readonly IBackgroundEventsQueue _eventsQueue;
        private readonly EventsService _events;
        private readonly IServiceProvider _services;

        public EventsQueueService(IBackgroundEventsQueue eventsQueue, ILogger<EventsQueueService> logger,
            EventsService events, IServiceProvider services)
        {
            _eventsQueue = eventsQueue;
            _logger = logger;
            _events = events;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Events Queue Service is running");
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            List<Task> tasks = new();

            while (!stoppingToken.IsCancellationRequested)
            {
                Func<EventsService, IServiceScope, CancellationToken, Task> item =
                    await _eventsQueue.DequeueAsync(stoppingToken);

                try
                {
                    IServiceScope scope = _services.CreateScope();
                    tasks.Add(
                        item(_events, _services.CreateScope(), stoppingToken)
                            .ContinueWith(x =>
                            {
                                lock (tasks)
                                {
                                    tasks.Remove(x);
                                }
                                scope.Dispose();
                            }, TaskContinuationOptions.None)
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        $"Error occurred executing {nameof(item)}.");
                }
            }

            await Task.WhenAll(tasks);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Events Queue Service is stopping.");

            return base.StopAsync(cancellationToken);
        }
    }
}
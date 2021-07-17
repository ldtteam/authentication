using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Api.Events
{
    public interface IBackgroundEventsQueue
    {
        Task QueueBackgroundWorkItemAsync(Func<EventsService, IServiceScope, CancellationToken, Task> workItem);

        ValueTask<Func<EventsService, IServiceScope, CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken);
    }
    
    public class BackgroundEventsQueue : IBackgroundEventsQueue
    {
        private readonly Channel<Func<EventsService, IServiceScope, CancellationToken, Task>> _queue;

        public BackgroundEventsQueue(int capacity)
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            BoundedChannelOptions options = new(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<EventsService, IServiceScope, CancellationToken, Task>>(options);
        }

        public async Task QueueBackgroundWorkItemAsync(
            Func<EventsService, IServiceScope, CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<Func<EventsService, IServiceScope, CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            Func<EventsService, IServiceScope, CancellationToken, Task> workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }
    }
}
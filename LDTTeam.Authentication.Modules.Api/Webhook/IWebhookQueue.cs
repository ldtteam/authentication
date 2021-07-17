using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.Modules.Api.Webhook
{
    public interface IWebhookQueue
    {
        Task QueueBackgroundWorkItemAsync(Embed embed);

        ValueTask<Embed> DequeueAsync(
            CancellationToken cancellationToken);
    }
    
    public class WebhookQueue : IWebhookQueue
    {
        private readonly Channel<Embed> _queue;

        public WebhookQueue(int capacity)
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
            _queue = Channel.CreateBounded<Embed>(options);
        }

        public async Task QueueBackgroundWorkItemAsync(
            Embed embed)
        {
            if (embed == null)
            {
                throw new ArgumentNullException(nameof(embed));
            }

            await _queue.Writer.WriteAsync(embed);
        }

        public async ValueTask<Embed> DequeueAsync(
            CancellationToken cancellationToken)
        {
            Embed embed = await _queue.Reader.ReadAsync(cancellationToken);

            return embed;
        }
    }
}
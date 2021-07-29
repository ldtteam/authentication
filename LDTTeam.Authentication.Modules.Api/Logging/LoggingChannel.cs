using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Remora.Discord.API.Objects;
using Channel = System.Threading.Channels.Channel;

namespace LDTTeam.Authentication.Modules.Api.Logging
{
    public interface ILoggingQueue
    {
        Task QueueBackgroundWorkItemAsync(Embed embed);

        ValueTask<Embed> DequeueAsync(CancellationToken cancellationToken);
    }

    public class LoggingQueue : ILoggingQueue
    {
        private readonly Channel<Embed> _queue;

        public LoggingQueue()
        {
            _queue = Channel.CreateUnbounded<Embed>();
        }

        public async Task QueueBackgroundWorkItemAsync(Embed embed)
        {
            if (embed == null)
            {
                throw new ArgumentNullException(nameof(embed));
            }

            await _queue.Writer.WriteAsync(embed);
        }

        public async ValueTask<Embed> DequeueAsync(CancellationToken cancellationToken)
        {
            Embed embed = await _queue.Reader.ReadAsync(cancellationToken);
            return embed;
        }
    }
}
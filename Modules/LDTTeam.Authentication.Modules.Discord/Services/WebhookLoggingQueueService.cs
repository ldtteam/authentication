using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Logging;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Modules.Discord.Services
{
    public class WebhookLoggingQueueService : BackgroundService
    {
        private readonly Channel<Embed> _embeds;

        public WebhookLoggingQueueService(Channel<Embed> embeds)
        {
            _embeds = embeds;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Gateway;

namespace LDTTeam.Authentication.Modules.Discord.Services
{
    public class DiscordBackgroundService : BackgroundService
    {
        private readonly DiscordGatewayClient _client;

        public DiscordBackgroundService(DiscordGatewayClient client)
        {
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _client.RunAsync(cancellationToken);
            }
        }
    }
}
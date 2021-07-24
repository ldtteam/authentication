using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Logging;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

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
            ConnectionFactory factory = new() {HostName = "localhost"};
            using IConnection connection = factory.CreateConnection();
            using IModel model = connection.CreateModel();

            model.QueueDeclare("embeds",
                false,
                false,
                false,
                null);

            while (!stoppingToken.IsCancellationRequested)
            {
                Embed embed = await _embeds.Reader.ReadAsync(stoppingToken);

                string message = JsonSerializer.Serialize(embed);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                
                model.BasicPublish("",
                    "embeds",
                    null,
                    messageBytes);
            }
        }
    }
}
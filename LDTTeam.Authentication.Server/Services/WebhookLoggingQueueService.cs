using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Webhook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Server.Services
{
    public class WebhookLoggingQueueService : BackgroundService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<WebhookLoggingQueueService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebhookQueue _loggingQueue;

        public WebhookLoggingQueueService(IWebhookQueue loggingQueue, ILogger<WebhookLoggingQueueService> logger, IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _loggingQueue = loggingQueue;
            _logger = logger;
            _configuration = configuration;
            _clientFactory = clientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Webhook Logging Queue Service is running");
            await BackgroundProcessing(stoppingToken);
        }
        
        private class WebhookRequest
        {
            [JsonPropertyName("embeds")]
            public List<Embed> Embeds { get; }

            public WebhookRequest(Embed embed)
            {
                Embeds = new List<Embed> {embed};
            }
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Embed embed = await _loggingQueue.DequeueAsync(stoppingToken);

                try
                {
                    HttpClient client = _clientFactory.CreateClient();

                    if (embed.Fields != null)
                    {
                        foreach (Embed.Field field in embed.Fields.Where(field => field.Value.Length >= 1024))
                        {
                            field.Value = $"{field.Value[..1021]}...";
                        }
                    }

                    HttpResponseMessage response = await client.PostAsJsonAsync(_configuration["WebHook"], new WebhookRequest(embed), stoppingToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogCritical($"Webhook Logging failed! {response.StatusCode} : {await response.Content.ReadAsStringAsync(stoppingToken)}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occurred executing.");
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Webhook Logging Queue Service is stopping.");

            return base.StopAsync(cancellationToken);
        }
    }
}
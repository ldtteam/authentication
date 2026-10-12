using LDTTeam.Authentication.DiscordBot.Config;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.DiscordBot.Service;

public partial class DiscordRetryFailedLogService(
    DiscordEventLoggingService logger, 
    IDiscordFailedLogQueueService queue,
    IOptions<DiscordConfig> discordConfig,
    ILogger<DiscordRetryFailedLogService> consoleLogger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var failedLogs = queue.Next();
            foreach (var (embed, count) in failedLogs)
            {
                if (count >= discordConfig.Value.MaxLogRetryAttempts)
                {
                    LogFailedToPostEmbedEmbedRetryCountExceeded(embed);
                    continue;
                }
                
                await logger.LogEvent(embed, count + 1);
            }
            await Task.Delay(5000, stoppingToken); // Wait for 5 seconds before checking the queue again
        }    
    }

    [LoggerMessage(LogLevel.Warning, "Failed to post embed: {Embed} retry count exceeded")]
    partial void LogFailedToPostEmbedEmbedRetryCountExceeded(Embed embed);
}
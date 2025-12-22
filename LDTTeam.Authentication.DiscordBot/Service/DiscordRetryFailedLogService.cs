using LDTTeam.Authentication.DiscordBot.Config;
using Microsoft.Extensions.Options;

namespace LDTTeam.Authentication.DiscordBot.Service;

public class DiscordRetryFailedLogService(
    DiscordEventLoggingService logger, IDiscordFailedLogQueueService queue,
    IOptions<DiscordConfig> discordConfig
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var failedLogs = queue.Next();
            foreach (var (embed, count) in failedLogs)
            {
                if (count >= discordConfig.Value.MaxLogRetryAttempts)
                    continue; // Skip logs that have exceeded max retry attempts
                
                await logger.LogEvent(embed, count + 1);
            }
            await Task.Delay(5000, stoppingToken); // Wait for 5 seconds before checking the queue again
        }    
    }
}
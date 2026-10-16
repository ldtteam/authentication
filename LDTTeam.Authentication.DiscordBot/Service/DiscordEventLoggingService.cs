using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;

namespace LDTTeam.Authentication.DiscordBot.Service;

public partial class DiscordEventLoggingService(
    ILoggingChannelProvider loggingChannelProvider,
    IDiscordRestChannelAPI channelApi,
    IDiscordFailedLogQueueService failedLogQueueService,
    ILogger<DiscordEventLoggingService> logger)
{

    public async Task LogEvent(Embed embed, int count = 0)
    {
        var channel = await loggingChannelProvider.GetLoggingChannelIdAsync();
        if (!channel.HasValue)
            return;

        var result = await SendEmbedAsync(channel.Value, embed);

        switch (result)
        {
            case { IsSuccess: false, Error: RestResultError<RestError> { Error.RetryAfter.HasValue: true } resultError }:
                LogFailedToLogEventWeHaveHitTheRateLimitQueueingForRetry();
                failedLogQueueService.EnqueueFailedLog(embed, resultError.Error, count);
                break;
            case { IsSuccess: false }:
                LogFailedToLogEventError(result.Error?.Message);
                break;
        }
    }
    
    private Task<Result<IMessage>> SendEmbedAsync
    (
        Snowflake channel,
        Embed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
        => SendAsync(channel, embeds: new[] { embed }, options: options, ct: ct);
    
    private Task<Result<IMessage>> SendAsync
    (
        Snowflake channel,
        Optional<string> content = default,
        Optional<IReadOnlyList<IEmbed>> embeds = default,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default
    )
    {
        return channelApi.CreateMessageAsync
        (
            channel,
            content: content,
            isTTS: options?.IsTTS ?? default,
            embeds: embeds,
            allowedMentions: options?.AllowedMentions ?? default,
            components: options?.MessageComponents ?? default,
            attachments: options?.Attachments ?? default,
            ct: ct
        );
    }

    [LoggerMessage(LogLevel.Information, $"Failed to log event. We have hit the rate limit. Queueing for retry!")]
    partial void LogFailedToLogEventWeHaveHitTheRateLimitQueueingForRetry();

    [LoggerMessage(LogLevel.Warning, "Failed to log event: {Error}")]
    partial void LogFailedToLogEventError(string? error);
}
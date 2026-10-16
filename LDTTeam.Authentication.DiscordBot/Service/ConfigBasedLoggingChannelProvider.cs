using LDTTeam.Authentication.DiscordBot.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Provides the logging channel Snowflake based on configuration values (uses <see cref="DiscordConfig"/>).
/// Uses <see cref="IMemoryCache"/> to cache the resolved Snowflake and reads configuration from an
/// <see cref="IOptionsSnapshot{TOptions}"/> so changes in configuration are picked up between requests.
/// </summary>
public partial class ConfigBasedLoggingChannelProvider(IOptions<DiscordConfig> configSnapshot, IMemoryCache cache, IDiscordRestGuildAPI guildApi, IServerProvider serverProvider, ILogger<ConfigBasedLoggingChannelProvider> logger) : ILoggingChannelProvider
{
    private const string CacheKey = "LoggingChannelSnowflake";

    public async Task<Snowflake?> GetLoggingChannelIdAsync()
    {
        if (cache.TryGetValue<Snowflake>(CacheKey, out var cached))
            return cached;

        // Read current config snapshot
        var config = configSnapshot.Value;
        var loggingChannel = config.LoggingChannel;
        
        // Look up channel by name
        var servers = await serverProvider.GetServersAsync();
        var server = servers[config.LoggingChannel.Server];
        var channels = await guildApi.GetGuildChannelsAsync(server);
        if (!channels.IsSuccess)
        {
            LogFailedToGetTheChannelsForLoggingErrormessage(channels.Error?.Message);
            return null;
        }
        
        var channel = channels.Entity.FirstOrDefault(c => c.Name == loggingChannel.Channel);
        if (channel == null)
        {
            LogCouldNotFindTheLoggingChannelWithNameLoggingchannelchannelAvailableAre(loggingChannel.Channel, string.Join(", ", channels.Entity.Select(e => e.Name)));
            return null;
        }
        
        // Cache for a short period
        cache.Set(CacheKey, channel.ID, TimeSpan.FromMinutes(5));
        return channel.ID;
    }

    [LoggerMessage(LogLevel.Error, "Failed to get the channels for logging: {ErrorMessage}")]
    partial void LogFailedToGetTheChannelsForLoggingErrormessage(string? errorMessage);

    [LoggerMessage(LogLevel.Warning, "Could not find the logging channel with name: {LoggingChannelChannel}. Available are: {AvailableChannels}")]
    partial void LogCouldNotFindTheLoggingChannelWithNameLoggingchannelchannelAvailableAre(string loggingChannelChannel, string availableChannels);
}

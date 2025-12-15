using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Provides the Discord channel Snowflake used for logging messages produced by the application or bot.
/// Implementations are responsible for locating the configured logging channel (for example from configuration
/// or server settings) and returning its <see cref="Snowflake"/> identifier.
/// </summary>
public interface ILoggingChannelProvider
{
    /// <summary>
    /// Asynchronously retrieves the Snowflake identifier of the logging channel.
    /// </summary>
    /// <remarks>
    /// Implementations should perform any necessary lookups (configuration, database, or remote API) and return
    /// the channel identifier. If a logging channel is not configured implementations may throw an exception or
    /// return a default/empty <see cref="Snowflake"/> depending on the project's conventions — callers should
    /// handle both cases appropriately.
    ///
    /// Implementations are encouraged to cache results where appropriate to avoid repeated lookups.
    /// </remarks>
    /// <returns>
    /// A <see cref="Task"/> that resolves to the <see cref="Snowflake"/> of the logging channel.
    /// </returns>
    public Task<Snowflake?> GetLoggingChannelIdAsync();
}
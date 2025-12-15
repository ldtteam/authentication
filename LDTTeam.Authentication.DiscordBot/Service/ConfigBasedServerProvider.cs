using LDTTeam.Authentication.DiscordBot.Config;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Provides server information based on configuration values.
/// </summary>
/// <remarks>
/// This implementation retrieves the server's display name and Snowflake ID from the application's configuration using <see cref="DiscordConfig"/>.
/// </remarks>
public class ConfigBasedServerProvider(IOptions<DiscordConfig> configSnapshot) : IServerProvider
{
    /// <summary>
    /// Asynchronously retrieves the server name and its unique Snowflake identifier from configuration.
    /// </summary>
    /// <returns>A tuple containing the server name as a string and the server's Snowflake ID.</returns>
    public ValueTask<(string Server, Snowflake Id)> GetServerAsync()
    {
        var config = configSnapshot.Value;
        return ValueTask.FromResult(
            (
                config.Server.DisplayName,
                config.Server.Snowflake
            )
        );
    }
}
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Provides an abstraction for retrieving server information, including the server name and its unique identifier.
/// </summary>
public interface IServerProvider
{
    /// <summary>
    /// Asynchronously retrieves the server names and their unique Snowflake identifier.
    /// </summary>
    public ValueTask<Dictionary<string, Snowflake>> GetServersAsync();

    /// <summary>
    /// Asynchronously retrieves the server IDs and their corresponding names.
    /// </summary>
    public async ValueTask<Dictionary<Snowflake, string>> GetServersByIdAsync()
    {
        var servers = await GetServersAsync();
        return servers.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
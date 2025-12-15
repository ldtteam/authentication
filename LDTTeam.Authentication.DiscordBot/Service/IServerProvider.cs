using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Provides an abstraction for retrieving server information, including the server name and its unique identifier.
/// </summary>
public interface IServerProvider
{
    /// <summary>
    /// Asynchronously retrieves the server name and its unique Snowflake identifier.
    /// </summary>
    /// <returns>A tuple containing the server name as a string and the server's Snowflake ID.</returns>
    public ValueTask<(string Server, Snowflake Id)> GetServerAsync();
}
using JetBrains.Annotations;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Config;

public record DiscordConfig(
    ServerConfig Server,
    string LoggingChannel,
    string BotToken
    );

[UsedImplicitly]
public record ServerConfig(string DisplayName, ulong Id)
{
    public Snowflake Snowflake => new(Id);
}
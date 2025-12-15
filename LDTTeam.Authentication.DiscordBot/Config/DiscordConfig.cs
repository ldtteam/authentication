using JetBrains.Annotations;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Config;

public class DiscordConfig
{
    public required ServerConfig Server { get; set; }
    public required string LoggingChannel { get; set; }
    public required string BotToken { get; set; }
}

[UsedImplicitly]
public class ServerConfig
{
    public Snowflake Snowflake => new(Id);
    public required string DisplayName { get; set; }
    public required ulong Id { get; set; }
}
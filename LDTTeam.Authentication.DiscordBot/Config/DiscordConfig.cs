using JetBrains.Annotations;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Config;

public class DiscordConfig
{
    public required Dictionary<string, ServerConfig> Server { get; set; }
    public required EventLoggingConfig LoggingChannel { get; set; }
    public required string BotToken { get; set; }
}

[UsedImplicitly]
public class ServerConfig
{
    public Snowflake Snowflake => new(Id);
    public required ulong Id { get; set; }
}

[UsedImplicitly]
public class EventLoggingConfig
{
    public required string Server { get; set; }
    public required string Channel { get; set; }
}
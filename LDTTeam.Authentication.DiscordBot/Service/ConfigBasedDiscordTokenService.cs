using LDTTeam.Authentication.DiscordBot.Config;
using Microsoft.Extensions.Options;
using Remora.Discord.Rest;

namespace LDTTeam.Authentication.DiscordBot.Service;

public class ConfigBasedDiscordTokenService(IOptions<DiscordConfig> config) : IAsyncTokenStore
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(config.Value.BotToken);
    }

    public DiscordTokenType TokenType => DiscordTokenType.Bot;
}
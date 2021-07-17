using System.Collections.Generic;
using LDTTeam.Authentication.Modules.Discord;
using LDTTeam.Authentication.Modules.GitHub;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Minecraft;
using LDTTeam.Authentication.Modules.Patreon;

namespace LDTTeam.Authentication.Server
{
    public class Modules
    {
        internal static readonly IReadOnlyList<IModule> List = new IModule[]
        {
            new GitHubModule(),
            new PatreonModule(),
            new DiscordModule(),
            new MinecraftModule()
        };
    }
}
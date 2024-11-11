using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Discord.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Services
{
    public class DiscordStartupTask : IStartupTask
    {
        private readonly SlashService _slashService;
        private readonly ILogger<DiscordBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public DiscordStartupTask(SlashService slashService, ILogger<DiscordBackgroundService> logger, IConfiguration configuration)
        {
            _slashService = slashService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            DiscordConfig? discordConfig = _configuration.GetSection("discord").Get<DiscordConfig>();

            if (discordConfig == null)
                throw new Exception("discord not set in configuration!");

            Result checkSlashSupport = await _slashService.UpdateSlashCommandsAsync(ct: cancellationToken);
            if (!checkSlashSupport.IsSuccess)
            {
                _logger.LogWarning
                (
                    "The registered commands of the bot don't support slash commands: {Reason}",
                    checkSlashSupport.Error.Message
                );
            }
            else
            {
                foreach (string server in discordConfig.RoleMappings.Keys)
                {
                    if (!ulong.TryParse(server, out ulong serverId)) continue;
                    
                    Result updateSlash = await _slashService.UpdateSlashCommandsAsync(new Snowflake(serverId), ct: cancellationToken);
                    if (!updateSlash.IsSuccess)
                    {
                        _logger.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
                    }
                }
            }
        }
    }
}
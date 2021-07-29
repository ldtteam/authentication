using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Services
{
    public class DiscordStartupTask : IStartupTask
    {
        private readonly SlashService _slashService;
        private readonly ILogger<DiscordBackgroundService> _logger;

        public DiscordStartupTask(SlashService slashService, ILogger<DiscordBackgroundService> logger)
        {
            _slashService = slashService;
            _logger = logger;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Result checkSlashSupport = _slashService.SupportsSlashCommands();
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
                Result updateSlash = _slashService.UpdateSlashCommandsAsync(new Snowflake(453039954386223145), cancellationToken).Result;
                if (!updateSlash.IsSuccess)
                {
                    _logger.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
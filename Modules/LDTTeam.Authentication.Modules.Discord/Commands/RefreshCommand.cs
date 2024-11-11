using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Utils;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    public class RefreshCommand : CommandGroup
    {
        private readonly IInteractionContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly IBackgroundEventsQueue _eventsQueue;

        public RefreshCommand(IInteractionContext context,
            IBackgroundEventsQueue eventsQueue, IFeedbackService feedbackService)
        {
            _context = context;
            _eventsQueue = eventsQueue;
            _feedbackService = feedbackService;
        }

        [Command("refresh")]
        [Description("Refreshes provider(s)")]
        public async Task<IResult> RemoveRewardConditionCommand([Description("Optional provider to refresh")]
            string? provider = null)
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var permissions = member.Value.Permissions;
            if (!permissions.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            if (!permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                return await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No Permission",
                        Description =
                            "You require Administrator permissions for this command",
                        Colour = Color.DarkRed
                    });
            }

            await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope,
                    provider == null ? null : [provider]);
                await events._postRefreshContentEvent.InvokeAsync(scope);
            }, CancellationToken);

            return await _feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = "Refreshed",
                    Description = "Refreshed provider(s)",
                    Colour = Color.Green
                });
        }
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Utils;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    public class RefreshCommand : CommandGroup
    {
        private readonly InteractionContext _context;
        private readonly IDiscordRestWebhookAPI _channelApi;
        private readonly IBackgroundEventsQueue _eventsQueue;

        public RefreshCommand(InteractionContext context, IDiscordRestWebhookAPI channelApi,
            IBackgroundEventsQueue eventsQueue)
        {
            _context = context;
            _channelApi = channelApi;
            _eventsQueue = eventsQueue;
        }

        [Command("refresh")]
        [Description("Refreshes provider(s)")]
        public async Task<IResult> RemoveRewardConditionCommand([Description("Optional provider to refresh")]
            string? provider = null)
        {
            if (!_context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                return await _channelApi.CreateFollowupMessageAsync
                (
                    _context.ApplicationID,
                    _context.Token,
                    embeds: new[]
                    {
                        new Embed
                        {
                            Title = "No Permission",
                            Description =
                                "You require Administrator permissions for this command",
                            Colour = Color.DarkRed
                        }
                    },
                    flags: MessageFlags.Ephemeral,
                    ct: CancellationToken
                );
            }

            await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope,
                    provider == null ? null : new List<string> {provider});
                await events._postRefreshContentEvent.InvokeAsync(scope);
            }, CancellationToken);

            return await _channelApi.CreateFollowupMessageAsync
            (
                _context.ApplicationID,
                _context.Token,
                "done!",
                flags: MessageFlags.Ephemeral,
                ct: CancellationToken
            );
        }
    }
}
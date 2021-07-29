using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Responders
{
    public class Responder : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;

        public Responder(IDiscordRestInteractionAPI interactionApi)
        {
            _interactionApi = interactionApi;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = new())
        {
            if (!gatewayEvent.Data.HasValue || !gatewayEvent.Data.Value.CustomID.HasValue) {
                return Result.FromSuccess();
            }

            await _interactionApi.CreateInteractionResponseAsync(gatewayEvent.ID, gatewayEvent.Token,
                new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage), ct);

            Console.WriteLine("TESTING");

            //await Reply(new Embed(Title: "testing"), new Optional<IReadOnlyList<IMessageComponent>>(), ct);

            return Result.FromSuccess();
        }

        /*pivate async Task<Result<IMessage>> Reply(Embed embed, Optional<IReadOnlyList<IMessageComponent>> components, CancellationToken ct = new())
        {
            return await _channelApi.CreateFollowupMessageAsync
            (
                _context.ApplicationID,
                _context.Token,
                embeds: new [] {embed},
                components: components,
                ct: ct
            );
        }*/
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    public class TestCommands : CommandGroup
    {
        private readonly InteractionContext _context;
        private readonly IDiscordRestWebhookAPI _channelApi;

        public TestCommands(InteractionContext context, IDiscordRestWebhookAPI channelApi)
        {
            _context = context;
            _channelApi = channelApi;
        }

        /// <summary>
        /// Posts a HTTP error code cat.
        /// </summary>
        /// <param name="httpCode">The HTTP error code.</param>
        /// <returns>The result of the command.</returns>
        [Command("cat")]
        [Description("Posts a cat image that represents the given error code.")]
        public async Task<Result> PostHttpCatAsync([Description("The HTTP code.")] int httpCode)
        {
            EmbedImage embedImage = new ($"https://http.cat/{httpCode}");
            ButtonComponent buttonComponent1 = new (ButtonComponentStyle.Link, "Label", URL: "https://google.com");
            ButtonComponent buttonComponent2 = new (ButtonComponentStyle.Primary, "Label", CustomID: "Test Button");
            ActionRowComponent actionRowComponent = new (new [] {buttonComponent1, buttonComponent2});
            Embed embed = new (Image: embedImage);

            Result<IMessage> reply = await Reply(embed, new [] {actionRowComponent});

            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }

        private async Task<Result<IMessage>> Reply(Embed embed, Optional<IReadOnlyList<IMessageComponent>> components)
        {
            return await _channelApi.CreateFollowupMessageAsync
            (
                _context.ApplicationID,
                _context.Token,
                embeds: new [] {embed},
                components: components,
                ct: CancellationToken
            );
        }
    }
}
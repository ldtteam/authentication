using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Rewards;
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
    public class MyRewardsCommands : CommandGroup
    {
        private readonly InteractionContext _context;
        private readonly IDiscordRestWebhookAPI _channelApi;
        private readonly IConditionService _conditionService;

        public MyRewardsCommands(InteractionContext context, IDiscordRestWebhookAPI channelApi,
            IConditionService conditionService)
        {
            _context = context;
            _channelApi = channelApi;
            _conditionService = conditionService;
        }

        [Command("myrewards")]
        [Description("Lists your LDTTeam Auth rewards")]
        public async Task<Result> MyRewardsCommand()
        {
            Dictionary<string, bool>? rewards =
                await _conditionService.GetRewardsForUser("discord", _context.User.ID.ToString(), CancellationToken);

            Result<IMessage> reply;
            if (rewards == null)
            {
                reply = await Reply(new Embed
                {
                    Title = "User not found",
                    Description =
                        $"User {_context.User.Username} was not found in our system, are you sure you've signed up?",
                    Colour = Color.Red
                }, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            else
            {
                List<IEmbedField> fields = new();
                
                foreach ((string reward, bool has) in rewards)
                {
                    fields.Add(new EmbedField(reward, has.ToString(), true));
                }

                Embed embed = new()
                {
                    Title = $"{_context.User.Username}'s rewards",
                    Colour = Color.Green,
                    Fields = fields
                };

                reply = await Reply(embed, new Optional<IReadOnlyList<IMessageComponent>>());
            }
            
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
                embeds: new[] {embed},
                components: components,
                ct: CancellationToken
            );
        }
    }
}
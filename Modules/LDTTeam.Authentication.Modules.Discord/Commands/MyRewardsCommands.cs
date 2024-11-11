using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Rewards;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace LDTTeam.Authentication.Modules.Discord.Commands
{
    public class MyRewardsCommands : CommandGroup
    {
        private readonly IInteractionContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly IConditionService _conditionService;

        public MyRewardsCommands(IInteractionContext context,
            IFeedbackService feedbackService,
            IConditionService conditionService)
        {
            _context = context;
            _feedbackService = feedbackService;
            _conditionService = conditionService;
        }

        [Command("myrewards")]
        [Description("Lists your LDTTeam Auth rewards")]
        public async Task<IResult> MyRewardsCommand()
        {
            var member = _context.Interaction.Member;
            if (!member.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            var user = member.Value.User;
            if (!user.HasValue)
            {
                return await _feedbackService.SendContextualErrorAsync(
                    "Command needs a user to run"
                );
            }
            
            var rewards =
                await _conditionService.GetRewardsForUser("discord", user.Value.ID.ToString(), CancellationToken);

            Result<IMessage> reply;
            if (rewards == null)
            {
                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "User not found",
                        Description =
                            $"User {user.Value.Username} was not found in our system, are you sure you've signed up?",
                        Colour = Color.Red
                    }
                );
            }
            else
            {
                List<IEmbedField> fields = new();
                
                foreach ((string reward, bool has) in rewards)
                {
                    fields.Add(new EmbedField(reward, has.ToString(), true));
                }

                reply = await _feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = $"{user.Value.Username}'s rewards",
                        Colour = Color.Green,
                        Fields = fields
                    }
                );
            }
            
            return !reply.IsSuccess
                ? Result.FromError(reply)
                : Result.FromSuccess();
        }
    }
}
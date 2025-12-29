using System.ComponentModel;
using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Model.Data;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Models.App.User;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using Wolverine;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Commands;

[Group("contribution")]
[UsedImplicitly]
public class ContributionRewards : CommandGroup
{
    [Group("user")]
    [UsedImplicitly]
    public class User(
        IInteractionContext interactionContext,
        IUserRepository userRepository,
        IMessageBus bus,
        IFeedbackService feedbackService) : CommandGroup
    {

        [Command("add")]
        [Description("Adds a contribution reward to a user.")]
        [UsedImplicitly]
        public async Task<IResult> AddRewardAsync(
            [Description("The user to add a contribution to")]
            IUser discordUser,
            [Description("The amount of cents to add")]
            int amountInCents
        )
        {
            var executor = interactionContext.Interaction.Member;
            var permissions = executor.FlatMap(m => m.Permissions);
            if (!permissions.HasValue)
            {
                return await feedbackService.SendContextualErrorAsync(
                    "You are not authorized to run this command for other users"
                );
            }
            
            if (permissions.HasValue && !permissions.Value.HasPermission(DiscordPermission.Administrator))
            {
                return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "No permission",
                        Description =
                            "You require Administrator permissions to run this command for other users",
                        Colour = Color.Red
                    }));
            }

            var user = await userRepository.GetBySnowflakeAsync(discordUser.ID);
            if (user == null)
            {
                return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                    new Embed
                    {
                        Title = "Missing User",
                        Description =
                            "The user does not have their discord account linked in the authentication system.",
                        Colour = Color.Red
                    }));
            }
        
            await bus.PublishAsync(new Messages.User.UserLifetimeContributionIncreased(
                user.UserId,
                AccountProvider.Discord,
                amountInCents
            ));
        
            return await feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = "Contribution Added",
                    Description = $"Successfully added '{amountInCents}' cents as contribution to user {discordUser.Username}",
                    Colour = Color.Green
                });
        }
    }
}
using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Service;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Commands;

public class ContextCommands(
    IInteractionContext interactionContext,
    IUserRepository userRepository,
    IAssignedRewardRepository assignedRewardRepository,
    IFeedbackService feedbackService) : CommandGroup
{
    [Command("List Rewards")]
    [CommandType(ApplicationCommandType.User)]
    [UsedImplicitly]
    public async Task<IResult> ListRewardsForSpecificUserViaContext(
        IUser user)
    {
        var executor = interactionContext.Interaction.Member;
        var executingUser = executor.FlatMap(m => m.User);
        var permissions = executor.FlatMap(m => m.Permissions);
        if (!permissions.HasValue &&
            !(executingUser.HasValue && executingUser.Value.ID.Equals(user.ID)))
        {
            return await feedbackService.SendContextualErrorAsync(
                "You are not authorized to run this command for other users"
            );
        }

        if (permissions.HasValue && executingUser.HasValue
                                 && !executingUser.Value.ID.Equals(user.ID)
                                 && !permissions.Value.HasPermission(DiscordPermission.Administrator))
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

        if (!executingUser.HasValue)
        {
            return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = "No user provided",
                    Description =
                        "You need to supply a user to get rewards for",
                    Colour = Color.Red
                }));
        }

        return await ListRewardsFor(user.ID, user.Username);
    }

    private async Task<IResult> ListRewardsFor(Snowflake discordUserId, string userName)
    {
        var user = await userRepository.GetBySnowflakeAsync(discordUserId);
        if (user == null)
        {
            return Result.FromError(await feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = "Unknown User",
                    Description =
                        $"The user {userName} is not registered in the authentication system. Ensure they have linked their Discord account.",
                    Colour = Color.Red
                }));
        }

        var rewards = await assignedRewardRepository.GetForUserAsync(user.UserId);
        var rewardsList = rewards.ToList();

        if (rewardsList.Count == 0)
        {
            await feedbackService.SendContextualEmbedAsync(
                new Embed
                {
                    Title = "No Rewards",
                    Description =
                        $"The user {userName} has no active rewards.",
                    Colour = Color.OrangeRed
                });
            return Result.FromSuccess();
        }

        await feedbackService.SendContextualEmbedAsync(
            new Embed
            {
                Title = "Active Rewards",
                Description =
                    $"The user {userName} has the following rewards active:",
                Colour = Color.Green,
                Fields = rewardsList.Select(reward => new EmbedField(
                    reward.Type.ToString(),
                    reward.Reward
                )).ToList()
            });
        return Result.FromSuccess();
    }
}
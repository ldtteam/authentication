using System.ComponentModel;
using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Extensions;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Messages.Rewards;
using LDTTeam.Authentication.Models.App.Rewards;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using Wolverine;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Commands
{
    [Group("rewards")]
    [UsedImplicitly]
    public class RewardsCommands(
        IMessageBus bus,
        IFeedbackService feedbackService) : CommandGroup
    {
        public class UserRewardsCommands(
            IInteractionContext interactionContext,
            IUserRepository userRepository,
            IAssignedRewardRepository assignedRewardRepository,
            IFeedbackService feedbackService) : CommandGroup
        {
            [Command("list")]
            [Description("List the rewards a user has")]
            [UsedImplicitly]
            public async Task<IResult> ListRewardsForSpecificUser(
                [Description("User to get rewards for")] IUser? user)
            {
                var executor = interactionContext.Interaction.Member;
                var executingUser = executor.FlatMap(m => m.User);
                var permissions = executor.FlatMap(m => m.Permissions);
                if (!permissions.HasValue && 
                    user != null &&
                    !(executingUser.HasValue && executingUser.Value.ID.Equals(user.ID)))
                {
                    return await feedbackService.SendContextualErrorAsync(
                        "You are not authorized to run this command for other users"
                    );
                }

                if (permissions.HasValue && executingUser.HasValue
                                         && user != null
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

                if (user == null && !executingUser.HasValue)
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
                
                if (user == null && executingUser.HasValue)
                {
                    user = executingUser.Value;
                }

                return await ListRewardsFor(user!.ID, user.Username);
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

        [Command("upsert")]
        [Description("Update or insert a new reward")]
        [UsedImplicitly]
        public async Task<IResult> UpsertReward(
            [Description("The type of the reward that is updated or added")]
            RewardType type,
            [Description("The name of the reward. Has to be unique within the rewards of the same type.")]
            string reward,
            [Description(
                "The evaluated lambda. Prefixed with \"(tiers, lifetime) => \". Returns true for awarding the reward.")]
            string lambda)
        {
            return await bus.ExecuteProtectedAsync(
                feedbackService,
                "Failed to upsert reward",
                async b =>
                {
                    await b.PublishAsync(new RewardCreatedOrUpdated(reward, type, lambda));

                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Upserted",
                            Description =
                                $"The reward '{reward}' of type '{type}' has been upserted successfully. The changes may take a few minutes to propagate to all services.",
                            Colour = Color.Green
                        });

                    return Result.FromSuccess();
                });
        }

        [Command("remove")]
        [Description("Remove a reward")]
        [UsedImplicitly]
        public async Task<IResult> RemoveReward(
            [Description("The type of the reward that is removed")]
            RewardType type,
            [Description("The name of the reward. Has to be unique within the rewards of the same type.")]
            string reward)
        {
            return await bus.ExecuteProtectedAsync(
                feedbackService,
                "Failed to remove reward",
                async b =>
                {
                    await b.PublishAsync(new RewardRemoved(reward, type));

                    await feedbackService.SendContextualEmbedAsync(
                        new Embed
                        {
                            Title = "Reward Removed",
                            Description =
                                $"The reward '{reward}' of type '{type}' has been removed successfully. The changes may take a few minutes to propagate to all services.",
                            Colour = Color.Green
                        });

                    return Result.FromSuccess();
                });
        }
    }
}
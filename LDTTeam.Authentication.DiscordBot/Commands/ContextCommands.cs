using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Service;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;
using Wolverine;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Commands;

public class ContextCommands(
    IInteractionContext interactionContext,
    IDiscordRestInteractionAPI interactionApi,
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

    [Command("Remove Tier")]
    [CommandType(ApplicationCommandType.User)]
    [UsedImplicitly]
    public async Task<IResult> RemoveTierFromUser(
        IUser discordUser)
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

        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(
                InteractionCallbackType.Modal,
                new(new InteractionModalCallbackData(
                    CustomIDHelpers.CreateModalIDWithState("remove-tier-from-user", user.UserId.ToString()),
                    "Provide the tier to remove",
                    [
                        new ActionRowComponent(
                        [
                            new TextInputComponent(
                                "tier",
                                TextInputStyle.Short,
                                "Tier",
                                1,
                                100,
                                true,
                                new Optional<string>(),
                                "Enter the tier to remove from the user"
                            )
                        ])
                    ]
                ))
            )
        );
    }
    
    [Command("Add Tier")]
    [CommandType(ApplicationCommandType.User)]
    [UsedImplicitly]
    public async Task<IResult> AddTierToUser(
        IUser discordUser)
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

        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(
                InteractionCallbackType.Modal,
                new(new InteractionModalCallbackData(
                    CustomIDHelpers.CreateModalIDWithState("add-tier-to-user", user.UserId.ToString()),
                    "Provide the tier to add",
                    [
                        new ActionRowComponent(
                        [
                            new TextInputComponent(
                                "tier",
                                TextInputStyle.Short,
                                "Tier",
                                1,
                                100,
                                true,
                                new Optional<string>(),
                                "Enter the tier to add to the user"
                            )
                        ])
                    ]
                ))
            )
        );
    }
    
    [Command("Add Contribution")]
    [CommandType(ApplicationCommandType.User)]
    [UsedImplicitly]
    public async Task<IResult> AddContributionToUser(
        IUser discordUser)
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

        return await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(
                InteractionCallbackType.Modal,
                new(new InteractionModalCallbackData(
                    CustomIDHelpers.CreateModalIDWithState("add-contribution-to-user", user.UserId.ToString()),
                    "How much has the user contributed?",
                    [
                        new ActionRowComponent(
                        [
                            new TextInputComponent(
                                "contribution",
                                TextInputStyle.Short,
                                "Contribution (in cents)",
                                1,
                                100,
                                true,
                                new Optional<string>(),
                                "Enter the contribution amount in cents"
                            )
                        ])
                    ]
                ))
            )
        );
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
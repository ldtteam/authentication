using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Messages.User;
using LDTTeam.Authentication.Models.App.User;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using Wolverine;

namespace LDTTeam.Authentication.DiscordBot.Interactions;

public class UserInteractions(
    IInteractionContext interactionContext,
    IDiscordRestInteractionAPI interactionApi,
    IUserRepository userRepository,
    IAssignedRewardRepository assignedRewardRepository,
    IMessageBus bus,
    IFeedbackService feedbackService) : InteractionGroup
{
        
    [Modal("add-tier-to-user")]
    [UsedImplicitly]
    public async Task<Result> OnAddTierToUser(string state, string tier)
    {
        var userId = Guid.Parse(state);
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result.FromError(await feedbackService.SendContextualErrorAsync(
                "User not found."
            ));
        }
        
        await bus.PublishAsync(new UserTiersAdded(userId, AccountProvider.Discord, [tier]));
        
        await feedbackService.SendContextualEmbedAsync(
            new Embed()
            {
                Title = "Tier Added",
                Description = $"Successfully added tier '{tier}' to user.",
                Colour = Color.Green
            });

        return Result.FromSuccess();
    }
    
    [Modal("remove-tier-from-user")]
    [UsedImplicitly]
    public async Task<Result> OnRemoveTierFromUser(string state, string tier)
    {
        var userId = Guid.Parse(state);
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result.FromError(await feedbackService.SendContextualErrorAsync(
                "User not found."
            ));
        }
        
        await bus.PublishAsync(new UserTiersRemoved(userId, AccountProvider.Discord, [tier]));
        
        await feedbackService.SendContextualEmbedAsync(
            new Embed()
            {
                Title = "Tier Removed",
                Description = $"Successfully removed tier '{tier}' to user.",
                Colour = Color.Green
            });

        return Result.FromSuccess();
    }
    
    [Modal("add-contribution-to-user")]
    [UsedImplicitly]
    public async Task<Result> AddContributionToUser(string state, string contribution)
    {
        var userId = Guid.Parse(state);
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Result.FromError(await feedbackService.SendContextualErrorAsync(
                "User not found."
            ));
        }
        
        if (!int.TryParse(contribution, out var amountInCents))
        {
            return Result.FromError(await feedbackService.SendContextualErrorAsync(
                "Invalid contribution amount. Needs to be an integer value."
            ));
        }
        
        await bus.PublishAsync(new UserLifetimeContributionIncreased(userId, AccountProvider.Discord, amountInCents));
        
        await feedbackService.SendContextualEmbedAsync(
            new Embed()
            {
                Title = "Tier Removed",
                Description = $"Successfully altered lifetime contributions with: {amountInCents}.",
                Colour = Color.Green
            });

        return Result.FromSuccess();
    }
}
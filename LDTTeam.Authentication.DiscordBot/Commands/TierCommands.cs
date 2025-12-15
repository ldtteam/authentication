using System.ComponentModel;
using System.Drawing;
using JetBrains.Annotations;
using LDTTeam.Authentication.DiscordBot.Service;
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

[Group("tier")]
public class TierCommands(
    IInteractionContext interactionContext,
    IMessageBus bus,
    IUserRepository userRepository,
    IFeedbackService feedbackService
    ) : CommandGroup {
    
    [Command("Add")]
    [Description("List the rewards a user has")]
    [UsedImplicitly]
    public async Task<IResult> AddTierToUser(
        [Description("The user to add the tier to")] IUser discordUser,
        [Description("Tier to add")] string tier)
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
        
        await bus.PublishAsync(new Messages.User.UserTiersAdded(
            user.UserId.ToString(),
            [tier]
        ));
        
        return await feedbackService.SendContextualEmbedAsync(
            new Embed
            {
                Title = "Tier Added",
                Description = $"Successfully added tier '{tier}' to user {discordUser.Username}",
                Colour = Color.Green
            });
    }
    
    [Command("Remove")]
    [Description("List the rewards a user has")]
    [UsedImplicitly]
    public async Task<IResult> RemoveTierToUser(
        [Description("The user to remove the tier from")] IUser discordUser,
        [Description("Tier to remove")] string tier)
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
        
        await bus.PublishAsync(new Messages.User.UserTiersAdded(
            user.UserId.ToString(),
            [tier]
        ));
        
        return await feedbackService.SendContextualEmbedAsync(
            new Embed
            {
                Title = "Tier Removed",
                Description = $"Successfully removed tier '{tier}' from user {discordUser.Username}",
                Colour = Color.Green
            });
    }
}
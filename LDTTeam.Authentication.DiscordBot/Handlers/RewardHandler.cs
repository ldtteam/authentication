using System.Drawing;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Messages.Rewards;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.DiscordBot.Handlers;

public class RewardHandler(IRewardRepository repository, DiscordEventLoggingService eventLoggingService)
{
    
    public async Task Handle(RewardCreatedOrUpdated message)
    {
        await repository.UpsertAsync(
            new Model.Data.Reward
            {
                Name = message.Reward,
                Type = message.Type
            });

        await eventLoggingService.LogEvent(new Embed()
        {
            Title = "Reward Created/Updated",
            Description = $"Reward `{message.Reward}` of type `{message.Type}` has been created/updated.",
            Colour = Color.CornflowerBlue
        });
    }
    
    public async Task Handle(RewardRemoved message)
    {
        await repository.DeleteAsync(message.Type, message.Reward);

        await eventLoggingService.LogEvent(new Embed()
        {
            Title = "Reward Deleted",
            Description = $"Reward `{message.Reward}` of type `{message.Type}` has been deleted.",
            Colour = Color.IndianRed
        });
    }
}
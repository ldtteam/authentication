using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

public class DiscordEventLoggingService(
    ILoggingChannelProvider loggingChannelProvider,
    IFeedbackService feedbackService
    )
{
    
    public async Task LogEvent(Embed embed)
    {
        var channel = await loggingChannelProvider.GetLoggingChannelIdAsync();
        if (!channel.HasValue)
            return;
        
        await feedbackService.SendEmbedAsync(channel.Value, embed);
    }
}
using System.Drawing;
using LDTTeam.Authentication.DiscordBot.Service;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace LDTTeam.Authentication.DiscordBot.Responders;

public partial class AssignRolesOnJoinResponder(
    DiscordRoleAssignmentService roleAssignmentService,
    DiscordEventLoggingService eventLoggingService,
    ILogger<AssignRolesOnJoinResponder> logger) : IResponder<IGuildMemberAdd>
{
    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new())
    {
        var user = gatewayEvent.User.OrDefault(null);
        if (user == null)
        {
            LogUserInformationMissingInGuildmemberaddEventForGuildidGuildid(logger, gatewayEvent.GuildID);
            return Result.FromError(new ExceptionError(new Exception("User information missing in GuildMemberAdd event")));
        }
        
        var assigner = await roleAssignmentService.ForMember(user.ID);
        await assigner.EnsureRewardsAssigned(ct);
        await eventLoggingService.LogEvent(
            new Embed()
            {
                Title = "User Joined Guild (Assigned Roles if applicable)",
                Description = "User **" + user.Username + "** (`" + user.ID +
                              "`) has joined the guild. Assigned roles as applicable.",
                Colour = Color.Chocolate
            });
        return Result.FromSuccess();
    }

    [LoggerMessage(LogLevel.Error, "User information missing in GuildMemberAdd event for GuildID: {guildId}")]
    static partial void LogUserInformationMissingInGuildmemberaddEventForGuildidGuildid(ILogger<AssignRolesOnJoinResponder> logger, Snowflake guildId);
}
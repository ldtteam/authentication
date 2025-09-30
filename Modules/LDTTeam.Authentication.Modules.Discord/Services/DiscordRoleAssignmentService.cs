using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Rewards;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.Modules.Discord.Services;

/// <summary>
/// Service which can assign roles to users in a Discord server.
/// </summary>
/// <param name="guildApi">The API</param>
/// <param name="roleRewardService">Service which handles the retrieval of role rewards.</param>
/// <param name="loggerFactory">The logger factory to construct loggers for the individual servers.</param>
/// <param name="logger">The logger for this service.</param>
public class DiscordRoleAssignmentService (
    IDiscordRestGuildAPI guildApi,
    DiscordRoleRewardService roleRewardService,
    ILoggerFactory loggerFactory,
    ILogger<DiscordRoleAssignmentService> logger
) {

    /// <summary>
    /// Role assigner for a specific server
    /// </summary>
    /// <param name="server">The server to assign the roles in.</param>
    public ServerRoleAssigner ForServer(Snowflake server)
    {
        logger.LogDebug("Creating role assigner for server {Server}", server);
        
        var assignerLogger = loggerFactory.CreateLogger<ServerRoleAssigner>();
        return new ServerRoleAssigner(guildApi, server, assignerLogger);
    }
    
    /// <summary>
    /// Role assigner for a specific member
    /// </summary>
    /// <param name="member">The member to assign roles to.</param>
    /// <returns>The member role assigner which can manipulate roles and rewards for that member</returns>
    public MemberRoleAssigner ForMember(Snowflake member)
    {
        logger.LogDebug("Creating role assigner for member {Member}", member);
        
        var assignerLogger = loggerFactory.CreateLogger<MemberRoleAssigner>();
        return new MemberRoleAssigner(this, roleRewardService, member, assignerLogger);
    }
    
    /// <summary>
    /// Role assigner for a specific server
    /// </summary>
    /// <param name="guildApi">The guild API to assign the roles with.</param>
    /// <param name="server">The server to assign the roles in.</param>
    public readonly struct ServerRoleAssigner(
        IDiscordRestGuildAPI guildApi,
        Snowflake server,
        ILogger<ServerRoleAssigner> logger
    ) {
        /// <summary>
        /// Assigns the specified role to the specified users in the server.
        /// </summary>
        /// <param name="role">The role to assign</param>
        /// <param name="user">The user to assign the role to</param>
        /// <param name="reason">The reason for the role assignment</param>
        /// <param name="token">The cancellation token to cancel the operation</param>
        public async Task AssignTo(Snowflake role, Snowflake user, string reason, CancellationToken token = default)
        {
            var result = await guildApi.AddGuildMemberRoleAsync(server, user, role, reason, token);
            if (!result.IsSuccess)
            {
                logger.LogError("Failed to assign role {Role} to user {User} in server {Server}: {Error}", role, user, server, result.Error);
            }
            else
            {
                logger.LogDebug("Assigned role {Role} to user {User} in server {Server}", role, user, server);
            }
        }

        /// <summary>
        /// Returns the roles assigned to the specified user in the server.
        /// </summary>
        /// <param name="user">The user to get the roles for.</param>
        /// <param name="token">The token to cancel the operation</param>
        /// <returns>The roles for this user.</returns>
        /// <exception cref="Exception">Thrown when the user could not be found in the server.</exception>
        public async Task<IReadOnlyList<Snowflake>> GetAssignedRoles(Snowflake user, CancellationToken token = default)
        {
            var result = await guildApi.GetGuildMemberAsync(server, user, token);
            if (result.IsSuccess) return result.Entity.Roles;
            
            logger.LogWarning("Failed to get roles for user {User} in server {Server}: {Error}", user, server, result.Error);
            return [];
        }
        
        /// <summary>
        /// Removes the specified role from the specified users in the server.
        /// </summary>
        /// <param name="role">The role to remove</param>
        /// <param name="user">The user to remove the role from</param>
        /// <param name="reason">The reason for the removal</param>
        /// <param name="token">The cancellation token.</param>
        public async Task RemoveFrom(Snowflake role, Snowflake user, string reason, CancellationToken token = default)
        {
            var result = await guildApi.RemoveGuildMemberRoleAsync(server, user, role, reason, token);
            if (!result.IsSuccess)
            {
                logger.LogError("Failed to remove role {Role} from user {User} in server {Server}: {Error}", role, user, server, result.Error);
            }
            else
            {
                logger.LogDebug("Removed role {Role} from user {User} in server {Server}", role, user, server);
            }
        }
    }

    /// <summary>
    /// Ensures that the specified member has all roles they should have based on their active rewards.
    /// </summary>
    /// <param name="service">The service to request server side role assignment from.</param>
    /// <param name="roleRewardService">The role reward service</param>
    /// <param name="member">The member to handle</param>
    /// <param name="logger">The logger</param>
    public readonly struct MemberRoleAssigner(
        DiscordRoleAssignmentService service,
        DiscordRoleRewardService roleRewardService,
        Snowflake member,
        ILogger<MemberRoleAssigner> logger)
    {

        public async Task EnsureRewardsAssigned(CancellationToken token = default)
        {
            logger.LogDebug("Ensuring reward roles are assigned for member {Member}", member);
            await foreach (var (server, role) in roleRewardService.Active(member, token))
            {
                await service.ForServer(server).AssignTo(role, member, "Ensuring reward role is assigned", token);
            }
        }
        
        public async Task RemoveAllRewards(CancellationToken token = default)
        {
            logger.LogDebug("Removing all reward roles from member {Member}", member);
            foreach (var (server, role) in roleRewardService.Rewards)
            {
                await service.ForServer(server).RemoveFrom(role, member, "Removing all reward roles", token);
            }
        }

        public async Task UpdateAllRewards(CancellationToken token = default)
        {
            logger.LogDebug("Updating all reward roles for member {Member}", member);
            var roles = roleRewardService.Rewards.ToHashSet();
            await foreach (var (server, role) in roleRewardService.Active(member, token))
            {
                await service.ForServer(server).AssignTo(role, member, "Ensuring reward role is assigned", token);
                roles.Remove((server, role));
            }
            
            foreach (var (server, role) in roles)
            {
                await service.ForServer(server).RemoveFrom(role, member, "Removing unearned reward role", token);
            }
        }
    }
}
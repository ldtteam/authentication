using LDTTeam.Authentication.DiscordBot.Extensions;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Service which can assign roles to users in a Discord server.
/// </summary>
public partial class DiscordRoleAssignmentService (
    IDiscordRestGuildAPI guildApi,
    IServerProvider serverProvider,
    IUserRepository userRepository,
    DiscordRoleRewardService roleRewardService,
    ILoggerFactory loggerFactory,
    ILogger<DiscordRoleAssignmentService> logger
) {

    /// <summary>
    /// Role assigner for a specific server
    /// </summary>
    private async Task<ServerRoleAssigner> ForServer(Snowflake id)
    {
        var servers = await serverProvider.GetServersByIdAsync();
        LogCreatingRoleAssignerForServerServer(logger, servers[id]);
        
        var assignerLogger = loggerFactory.CreateLogger<ServerRoleAssigner>();
        return new ServerRoleAssigner(guildApi, id, assignerLogger);
    }
    
    /// <summary>
    /// Role assigner for a specific member
    /// </summary>
    /// <param name="member">The member to assign roles to.</param>
    /// <returns>The member role assigner which can manipulate roles and rewards for that member</returns>
    public Task<MemberRoleAssigner> ForMember(Snowflake member)
    {
        LogCreatingRoleAssignerForMemberMember(logger, member);

        var assignerLogger = loggerFactory.CreateLogger<MemberRoleAssigner>();
        return Task.FromResult(new MemberRoleAssigner(roleRewardService, member, this, assignerLogger));
    }
    
    /// <summary>
    /// Role assigner for all members
    /// </summary>
    public Task<AllMembersRoleAssigner> ForAllMembers()
    {
        return Task.FromResult(new AllMembersRoleAssigner(this, userRepository));
    }
    
    /// <summary>
    /// Role assigner for a specific server
    /// </summary>
    /// <param name="guildApi">The guild API to assign the roles with.</param>
    /// <param name="server">The server to assign the roles in.</param>
    public partial class ServerRoleAssigner(
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
            var result = await guildApi.Retry(async (api, tkn) =>
                    await api.AddGuildMemberRoleAsync(server, user, role, reason, tkn),
                cancellationToken: token
            );
            
            if (!result.IsSuccess)
            {
                LogFailedToAssignRoleRoleToUserUserInServerServerError(logger, role, user, server, result.Error);
            }
            else
            {
                LogAssignedRoleRoleToUserUserInServerServer(logger, role, user, server);
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
            var result = await guildApi.Retry(async (api, tkn) =>
                    await api.GetGuildMemberAsync(server, user, tkn),
                cancellationToken: token
            );
            
            if (result.IsSuccess) return result.Entity.Roles;
            
            LogFailedToGetRolesForUserUserInServerServerError(logger, user, server, result.Error);
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
            var result = await guildApi.Retry(async (api, tkn) =>
                await api.RemoveGuildMemberRoleAsync(server, user, role, reason, tkn),
                cancellationToken: token
            );
            
            if (!result.IsSuccess)
            {
                LogFailedToRemoveRoleRoleFromUserUserInServerServerError(logger, role, user, server, result.Error);
            }
            else
            {
                LogRemovedRoleRoleFromUserUserInServerServer(logger, role, user, server);
            }
        }

        [LoggerMessage(LogLevel.Error, "Failed to assign role {role} to user {user} in server {server}: {error}")]
        static partial void LogFailedToAssignRoleRoleToUserUserInServerServerError(ILogger<ServerRoleAssigner> logger, Snowflake role, Snowflake user, Snowflake server, IResultError error);

        [LoggerMessage(LogLevel.Debug, "Assigned role {role} to user {user} in server {server}")]
        static partial void LogAssignedRoleRoleToUserUserInServerServer(ILogger<ServerRoleAssigner> logger, Snowflake role, Snowflake user, Snowflake server);

        [LoggerMessage(LogLevel.Warning, "Failed to get roles for user {user} in server {server}: {error}")]
        static partial void LogFailedToGetRolesForUserUserInServerServerError(ILogger<ServerRoleAssigner> logger, Snowflake user, Snowflake server, IResultError error);

        [LoggerMessage(LogLevel.Error, "Failed to remove role {role} from user {user} in server {server}: {error}")]
        static partial void LogFailedToRemoveRoleRoleFromUserUserInServerServerError(ILogger<ServerRoleAssigner> logger, Snowflake role, Snowflake user, Snowflake server, IResultError error);

        [LoggerMessage(LogLevel.Debug, "Removed role {role} from user {user} in server {server}")]
        static partial void LogRemovedRoleRoleFromUserUserInServerServer(ILogger<ServerRoleAssigner> logger, Snowflake role, Snowflake user, Snowflake server);
    }

    /// <summary>
    /// Ensures that the specified member has all roles they should have based on their active rewards.
    /// </summary>
    public partial class MemberRoleAssigner(
        DiscordRoleRewardService roleRewardService,
        Snowflake member,
        DiscordRoleAssignmentService roleAssignmentService,
        ILogger<MemberRoleAssigner> logger)
    {
        public async Task EnsureRewardsAssigned(CancellationToken token = default)
        {
            LogEnsuringRewardRolesAreAssignedForMemberMember(logger, member);
            await foreach (var role in roleRewardService.ActiveRoles(member, token))
            {
                var assigner = await roleAssignmentService.ForServer(role.Server);
                await assigner.AssignTo(role.Role, member, "Ensuring reward role is assigned", token);
            }
        }
        
        public async Task RemoveAllRewards(CancellationToken token = default)
        {
            LogRemovingAllRewardRolesFromMemberMember(logger, member);
            await foreach (var role in roleRewardService.AllRoles(token))
            {
                var assigner = await roleAssignmentService.ForServer(role.Server);
                await assigner.RemoveFrom(role.Role, member, "Removing all reward roles", token);
            }
        }

        public async Task UpdateAllRewards(CancellationToken token = default)
        {
            LogUpdatingAllRewardRolesForMemberMember(logger, member);
            var roles = (await roleRewardService.AllRoles(token).ToListAsync(cancellationToken: token))
                .ToHashSet();
            await foreach (var role in roleRewardService.ActiveRoles(member, token))
            {
                var assigner = await roleAssignmentService.ForServer(role.Server);
                await assigner.AssignTo(role.Role, member, "Ensuring reward role is assigned", token);
                roles.Remove(role);
            }
            
            foreach (var role in roles)
            {
                var assigner = await roleAssignmentService.ForServer(role.Server);
                await assigner.RemoveFrom(role.Role, member, "Removing unearned reward role", token);
            }
        }

        [LoggerMessage(LogLevel.Debug, "Ensuring reward roles are assigned for member {member}")]
        static partial void LogEnsuringRewardRolesAreAssignedForMemberMember(ILogger<MemberRoleAssigner> logger, Snowflake member);

        [LoggerMessage(LogLevel.Debug, "Removing all reward roles from member {member}")]
        static partial void LogRemovingAllRewardRolesFromMemberMember(ILogger<MemberRoleAssigner> logger, Snowflake member);

        [LoggerMessage(LogLevel.Debug, "Updating all reward roles for member {member}")]
        static partial void LogUpdatingAllRewardRolesForMemberMember(ILogger<MemberRoleAssigner> logger, Snowflake member);
    }

    public class AllMembersRoleAssigner(
        DiscordRoleAssignmentService roleAssignmentService,
        IUserRepository userRepository
    )
    {
        public async Task EnsureRewardsAssigned(CancellationToken token = default)
        {
            var allUsers = await userRepository.GetAllUserSnowflakesAsync(token);
            foreach (var user in allUsers)
            {
                var memberAssigner = await roleAssignmentService.ForMember(user);
                await memberAssigner.EnsureRewardsAssigned(token);
            }
        }
        
        public async Task RemoveAllRewards(CancellationToken token = default)
        {
            var allUsers = await userRepository.GetAllUserSnowflakesAsync(token);
            foreach (var user in allUsers)
            {
                var memberAssigner = await roleAssignmentService.ForMember(user);
                await memberAssigner.RemoveAllRewards(token);
            }
        }

        public async Task UpdateAllRewards(CancellationToken token = default)
        {
            var allUsers = await userRepository.GetAllUserSnowflakesAsync(token);
            foreach (var user in allUsers)
            {
                var memberAssigner = await roleAssignmentService.ForMember(user);
                await memberAssigner.UpdateAllRewards(token);
            }
        }
    }

    [LoggerMessage(LogLevel.Debug, "Creating role assigner for server {server}")]
    static partial void LogCreatingRoleAssignerForServerServer(ILogger<DiscordRoleAssignmentService> logger, string server);

    [LoggerMessage(LogLevel.Debug, "Creating role assigner for member {member}")]
    static partial void LogCreatingRoleAssignerForMemberMember(ILogger<DiscordRoleAssignmentService> logger, Snowflake member);
}
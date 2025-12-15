using System.Runtime.CompilerServices;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

public record DiscordRoleRewardService(
    IUserRepository UserRepository,
    IRoleRewardRepository RoleRewardRepository,
    IAssignedRewardRepository AssignedRewardRepository
    ) {

    public async IAsyncEnumerable<Snowflake> AllRoles([EnumeratorCancellation] CancellationToken token)
    {
        var allMappings = await RoleRewardRepository.GetAllAsync(token);
        foreach (var mapping in allMappings)
        {
            yield return mapping.Role;
        }
    }
    
    public async IAsyncEnumerable<Snowflake> ActiveRoles(Snowflake member, [EnumeratorCancellation] CancellationToken token)
    {
        var user = await UserRepository.GetBySnowflakeAsync(member, token);
        if (user is null)
            yield break;

        var assignedRewards = await AssignedRewardRepository.GetForUserAsync(user.UserId, token);
        foreach (var assignedReward in assignedRewards)
        {
            var roles = await RoleRewardRepository.GetRoleForRewardAsync(assignedReward.Reward, token);
            foreach (var role in roles)
            {
                yield return role;
            }
        }
    }
}
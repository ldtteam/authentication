using LDTTeam.Authentication.DiscordBot.Model.Data;
using Remora.Rest.Core;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Repository for mapping application reward names to Discord role Snowflakes.
/// </summary>
public interface IRoleRewardRepository
{
    /// <summary>
    /// Gets the role associated with the reward name or <c>null</c> if not mapped.
    /// </summary>
    Task<IEnumerable<(Snowflake Role, Snowflake Server)>> GetRoleForRewardAsync(string reward, CancellationToken token = default);

    /// <summary>
    /// Returns all configured reward->role mappings.
    /// </summary>
    Task<IEnumerable<RoleRewards>> GetAllAsync(CancellationToken token = default);

    /// <summary>
    /// Adds or updates a reward->role mapping.
    /// </summary>
    Task UpsertAsync(RoleRewards mapping, CancellationToken token = default);

    /// <summary>
    /// Removes the mapping for the specified reward if present.
    /// </summary>
    Task RemoveAsync(string reward, Snowflake role, Snowflake server, CancellationToken token = default);

    /// <summary>
    /// Removes all mappings for the specified reward.
    /// </summary>
    Task RemoveAllAsync(string reward, CancellationToken token = default);
}


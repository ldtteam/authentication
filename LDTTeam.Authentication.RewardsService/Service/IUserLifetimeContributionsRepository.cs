using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for managing user lifetime contributions in the rewards service.
/// Provides methods to add/update, remove, and retrieve contributions associated with a user.
/// </summary>
public interface IUserLifetimeContributionsRepository
{
    /// <summary>
    /// Asynchronously sets the lifetime contribution for the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="contribution">The lifetime contribution value to set.</param>
    Task SetUserLifetimeContributionAsync(Guid userId, decimal contribution);

    /// <summary>
    /// Asynchronously retrieves the lifetime contribution for the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The lifetime contribution value for the user, or 0 if not found.</returns>
    Task<decimal> GetUserLifetimeContributionAsync(Guid userId);

    /// <summary>
    /// Asynchronously removes the lifetime contribution record for the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    Task RemoveUserLifetimeContributionAsync(Guid userId);
    
    /// <summary>
    /// Asynchronously retrieves all user IDs that have lifetime contributions recorded.
    /// </summary>
    /// <returns>The list of users with contributions</returns>
    Task <List<Guid>> GetAllUsers();
}


namespace LDTTeam.Authentication.RewardsService.Service;

/// <summary>
/// Interface for managing user tiers in the rewards service.
/// Provides methods to add, remove, and retrieve tiers associated with a user.
/// </summary>
public interface IUserTiersRepository
{
    /// <summary>
    /// Asynchronously adds the specified tiers to the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tiers">A list of tier names to add to the user.</param>
    public Task AddUserTiersAsync(Guid userId, List<string> tiers);

    /// <summary>
    /// Asynchronously removes the specified tiers from the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tiers">A list of tier names to remove from the user.</param>
    public Task RemoveUserTiersAsync(Guid userId, List<string> tiers);

    /// <summary>
    /// Asynchronously retrieves the list of tiers associated with the user with the given userId.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of tier names associated with the user.</returns>
    public Task<List<string>> GetUserTiersAsync(Guid userId);

    /// <summary>
    /// Asynchronously retrieves all user IDs that have any tiers assigned.
    /// </summary>
    /// <returns>The list of all user ids</returns>
    public Task<List<Guid>> GetAllUsers();
}
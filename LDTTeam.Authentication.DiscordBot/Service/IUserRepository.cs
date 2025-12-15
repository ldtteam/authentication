using Remora.Rest.Core;
using LDTTeam.Authentication.DiscordBot.Model.Data;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Repository-style service for working with <see cref="User"/> entities.
/// All methods are asynchronous and use the application's <see cref="Data.DatabaseContext"/> as the backing store.
/// Implementations should use <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> to reduce database load.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by the application's internal <see cref="User.UserId"/>.
    /// </summary>
    /// <param name="userId">The user's GUID identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken token = default);

    /// <summary>
    /// Gets a user by their Discord <see cref="User.Snowflake"/>.
    /// </summary>
    /// <param name="snowflake">The Discord snowflake.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The <see cref="User"/> or <c>null</c> if not found.</returns>
    Task<User?> GetBySnowflakeAsync(Snowflake snowflake, CancellationToken token = default);

    /// <summary>
    /// Creates or updates a user in the database.
    /// On success the returned <see cref="User"/> will reflect any values stored.
    /// </summary>
    /// <param name="user">The user to create or update.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The upserted <see cref="User"/>.</returns>
    Task<User> CreateOrUpdateAsync(User user, CancellationToken token = default);

    /// <summary>
    /// Deletes the user with the specified id if it exists.
    /// </summary>
    /// <param name="userId">The user's GUID identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(Guid userId, CancellationToken token = default);
}


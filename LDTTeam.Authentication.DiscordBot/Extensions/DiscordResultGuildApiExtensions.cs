using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Results;
using Remora.Results;
using IResult = Remora.Results.IResult;

namespace LDTTeam.Authentication.DiscordBot.Extensions;

/// <summary>
/// Provides extension methods for retrying Discord REST API actions with configurable retry logic.
/// </summary>
public static class DiscordResultGuildApiExtensions
{
    /// <summary>
    /// Retries the specified asynchronous action up to a maximum number of attempts, returning the first successful result or the last failure.
    /// </summary>
    /// <param name="api">The Discord REST Guild API instance (unused, for extension method syntax).</param>
    /// <param name="action">The asynchronous action to retry.</param>
    /// <param name="maxRetries">The maximum number of retry attempts. Defaults to 3.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The first successful <see cref="Result"/>, or the last failure if all attempts fail.</returns>
    public static async Task<T> Retry<T>(
        this IDiscordRestGuildAPI api,
        Func<IDiscordRestGuildAPI, CancellationToken, Task<T>> action,
        int maxRetries = 3,
        CancellationToken cancellationToken = default
    ) where T : IResult
    {
        var attempt = 0;
        T result;

        do
        {
            result = await action(api, cancellationToken);

            if (result is { IsSuccess: false, Error: RestResultError<RestError> { Error.RetryAfter.HasValue: true } resultError })
            {
                var delay = resultError.Error.RetryAfter.Value;
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            } else switch (result.IsSuccess)
            {
                case true:
                    return result;
                case false:
                    attempt++;
                    break;
            }
        } while (attempt < maxRetries);

        return result;
    }
}
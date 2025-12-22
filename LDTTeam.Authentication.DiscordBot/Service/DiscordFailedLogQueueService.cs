using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace LDTTeam.Authentication.DiscordBot.Service;

/// <summary>
/// Service interface for managing a queue of failed Discord log entries that need to be retried.
/// </summary>
public interface IDiscordFailedLogQueueService
{
    /// <summary>
    /// Enqueues a failed log entry with the associated retry error information.
    /// </summary>
    /// <param name="embed">The Discord embed representing the log entry.</param>
    /// <param name="error">The error containing retry information.</param>
    void EnqueueFailedLog(Embed embed, IRestError error, int count);

    /// <summary>
    /// Retrieves the next set of embeds that are ready to be retried, removing them from the queue.
    /// </summary>
    /// <returns>An enumerable of embeds ready for retry.</returns>
    IEnumerable<(Embed Embed, int Count)> Next();
}

/// <summary>
/// Implementation of a queue for failed Discord log entries, supporting retry logic based on error retry intervals.
/// </summary>
public class DiscordFailedLogQueueService : IDiscordFailedLogQueueService
{
    /// <summary>
    /// Internal queue mapping the next attempt time to the embed to be retried.
    /// </summary>
    private readonly SortedList<DateTime, (Embed Embed, int count)> _queue = new();

    /// <inheritdoc />
    public void EnqueueFailedLog(Embed embed, IRestError error, int count)
    {
        var nextAttempt = DateTime.Now + TimeSpan.FromSeconds(error.RetryAfter.OrDefault(0f));
        _queue.Add(nextAttempt, (embed, count));
    }

    /// <inheritdoc />
    public IEnumerable<(Embed Embed, int Count)> Next()
    {
        if (_queue.Count <= 0)
            yield break;
        
        var now = DateTime.Now;
        
        // Copy keys to avoid modifying collection during enumeration
        var embeds = new List<(Embed Embed, int Count)>();
        foreach (var kvp in _queue)
        {
            if (kvp.Key <= now)
            {
                embeds.Add(kvp.Value);
            }
            else
            {
                break;
            }
        }

        // Remove processed embeds from the queue
        foreach (var embed in embeds)
        {
            _queue.RemoveAt(0);
            yield return embed;
        }
    }
}
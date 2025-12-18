using System.Security.Cryptography;
using System.Text;

namespace LDTTeam.Authentication.PatreonWebhookService.Service;

/// <summary>
/// Provides methods for generating and validating HMAC signatures and timestamps.
/// </summary>
public interface IHmacAuthenticationService
{
    /// <summary>
    /// Generates an HMAC signature for the given payload and secret.
    /// </summary>
    /// <param name="payload">The data to sign.</param>
    /// <param name="secret">The secret key used for signing.</param>
    /// <param name="algorithm">The HMAC algorithm (default: sha256).</param>
    /// <returns>The generated signature as a lowercase hex string.</returns>
    string GenerateSignature(string payload, string secret, string algorithm = "sha256");

    /// <summary>
    /// Validates an HMAC signature against the expected value.
    /// </summary>
    /// <param name="payload">The data to verify.</param>
    /// <param name="signature">The signature to validate.</param>
    /// <param name="secret">The secret key used for signing.</param>
    /// <param name="algorithm">The HMAC algorithm (default: sha256).</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    bool ValidateSignature(string payload, string signature, string secret, string algorithm = "sha256");

    /// <summary>
    /// Generates a Unix timestamp (seconds since epoch) as a string.
    /// </summary>
    /// <returns>The current Unix timestamp as a string.</returns>
    string GenerateTimestamp();

    /// <summary>
    /// Validates a Unix timestamp against the current time with a tolerance.
    /// </summary>
    /// <param name="timestamp">The timestamp to validate.</param>
    /// <param name="tolerance">The allowed time difference (default: 5 minutes).</param>
    /// <returns>True if the timestamp is within the tolerance; otherwise, false.</returns>
    bool ValidateTimestamp(string timestamp, TimeSpan tolerance = default);
}

/// <summary>
/// Implementation of <see cref="IHmacAuthenticationService"/> for HMAC-based authentication.
/// Provides methods to generate and validate HMAC signatures and timestamps for secure communication.
/// </summary>
public class HmacAuthenticationService : IHmacAuthenticationService
{
    private readonly ILogger<HmacAuthenticationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HmacAuthenticationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public HmacAuthenticationService(ILogger<HmacAuthenticationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string GenerateSignature(string payload, string secret, string algorithm = "sha256")
    {
        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            using HMAC hmac = algorithm.ToLowerInvariant() switch
            {
                "sha1" => new HMACSHA1(),
                "sha256" => new HMACSHA256(),
                "sha512" => new HMACSHA512(),
                "md5" => new HMACMD5(),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };
            hmac.Key = keyBytes;
            var hashBytes = hmac.ComputeHash(payloadBytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HMAC signature");
            throw;
        }
    }

    /// <inheritdoc/>
    public bool ValidateSignature(string payload, string signature, string secret, string algorithm = "sha256")
    {
        try
        {
            // Remove algorithm prefix if present (e.g., "sha256=" from GitHub)
            var cleanSignature = signature.Contains('=')
                ? signature.Split('=')[1]
                : signature;
            var expectedSignature = GenerateSignature(payload, secret, algorithm);

            // Use time-constant comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(cleanSignature),
                Encoding.UTF8.GetBytes(expectedSignature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HMAC signature");
            return false;
        }
    }

    /// <inheritdoc/>
    public string GenerateTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    /// <inheritdoc/>
    public bool ValidateTimestamp(string timestamp, TimeSpan tolerance = default)
    {
        if (tolerance == TimeSpan.Zero)
            tolerance = TimeSpan.FromMinutes(5); // Default 5-minute tolerance
        try
        {
            if (!long.TryParse(timestamp, out var unixTimestamp))
                return false;
            var providedTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            var now = DateTimeOffset.UtcNow;

            return Math.Abs((now - providedTime).TotalMilliseconds) <= tolerance.TotalMilliseconds;
        }
        catch
        {
            return false;
        }
    }
}
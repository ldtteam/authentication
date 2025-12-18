namespace LDTTeam.Authentication.PatreonWebhookService.Config;

/// <summary>
/// Configuration options for webhook authentication middleware.
/// </summary>
/// <remarks>
/// This class defines the settings required to validate incoming webhook requests using HMAC signatures.
/// </remarks>
public class WebhookAuthenticationConfig
{
    /// <summary>
    /// The shared secret used to generate and validate HMAC signatures.
    /// </summary>
    public required string Secret { get; set; } = "";

    /// <summary>
    /// The HMAC algorithm to use (e.g., "sha256", "sha1", "sha512").
    /// </summary>
    public required string Algorithm { get; set; } = "md5";

    /// <summary>
    /// The name of the HTTP header containing the HMAC signature.
    /// </summary>
    public required string SignatureHeaderName { get; set; } = "";

    /// <summary>
    /// The name of the HTTP header containing the timestamp (optional).
    /// If null, timestamp validation is not performed.
    /// </summary>
    public required string? TimestampHeaderName { get; set; } = null;
}
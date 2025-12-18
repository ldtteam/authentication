using System.Text;
using LDTTeam.Authentication.PatreonWebhookService.Config;
using LDTTeam.Authentication.PatreonWebhookService.Service;
using Microsoft.Extensions.Options;

namespace LDTTeam.Authentication.PatreonWebhookService.Middleware;

/// <summary>
/// Middleware for authenticating incoming webhook requests using HMAC signatures and optional timestamps.
/// Applies only to POST requests on endpoints starting with "/webhooks".
/// </summary>
/// <remarks>
/// This middleware validates the request body against an HMAC signature provided in a configurable header.
/// Optionally, it also validates a timestamp header to prevent replay attacks.
/// If authentication fails, a 401 Unauthorized response is returned.
/// </remarks>
public class WebhookAuthenticationMiddleware
{
    private readonly RequestDelegate next;
    private readonly IHmacAuthenticationService hmacService;
    private readonly IOptions<WebhookAuthenticationConfig> config;
    private readonly ILogger<WebhookAuthenticationMiddleware> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookAuthenticationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hmacService">The HMAC authentication service.</param>
    /// <param name="config">The webhook authentication configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public WebhookAuthenticationMiddleware(
        RequestDelegate next,
        IHmacAuthenticationService hmacService,
        IOptions<WebhookAuthenticationConfig> config,
        ILogger<WebhookAuthenticationMiddleware> logger)
    {
        this.next = next;
        this.hmacService = hmacService;
        this.config = config;
        this.logger = logger;
    }

    /// <summary>
    /// Processes an HTTP request and authenticates it if it targets a webhook endpoint.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to webhook endpoints
        if (!context.Request.Path.StartsWithSegments("/webhook"))
        {
            await next(context);
            return;
        }

        //We only authenticate POST requests
        if (context.Request.Method != HttpMethods.Post)
        {
            await next(context);
            return;
        }

        try
        {
            // Enable request body buffering for multiple reads
            context.Request.EnableBuffering();
            // Read the request body
            var body = await ReadRequestBodyAsync(context.Request);
            // Extract authentication headers
            var signature = ExtractSignature(context.Request.Headers);
            var timestamp = ExtractTimestamp(context.Request.Headers);
            var secret = config.Value.Secret;
            var algorithm = config.Value.Algorithm;
            
            // Check for missing signature
            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Missing required authentication headers");
                await WriteUnauthorizedResponse(context);
                return;
            }

            // Validate timestamp if provided
            if (!string.IsNullOrEmpty(timestamp))
            {
                if (!hmacService.ValidateTimestamp(timestamp))
                {
                    logger.LogWarning("Invalid or expired timestamp: {Timestamp}", timestamp);
                    await WriteUnauthorizedResponse(context);
                    return;
                }
            }

            // Validate HMAC signature
            var isValid = hmacService.ValidateSignature(body, signature, secret, algorithm);
            if (!isValid)
            {
                logger.LogWarning("Invalid HMAC signature for webhook request");
                await WriteUnauthorizedResponse(context);
                return;
            }

            // Reset body position for downstream middleware
            context.Request.Body.Position = 0;
            // Authentication successful, continue pipeline
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during webhook authentication");
            await WriteErrorResponse(context);
        }
    }

    /// <summary>
    /// Reads the request body as a string and resets the stream position.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The request body as a string.</returns>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    /// <summary>
    /// Extracts the HMAC signature from the request headers using the configured header name.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <returns>The signature value, or null if not found.</returns>
    private string? ExtractSignature(IHeaderDictionary headers)
    {
        // Support multiple signature header formats
        return headers.TryGetValue(config.Value.SignatureHeaderName, out var signature) ? signature.ToString() : null;
    }

    /// <summary>
    /// Extracts the timestamp from the request headers using the configured header name, if set.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <returns>The timestamp value, or null if not found or not configured.</returns>
    private string? ExtractTimestamp(IHeaderDictionary headers)
    {
        if (config.Value.TimestampHeaderName == null)
            return null;
        
        return headers.TryGetValue(config.Value.TimestampHeaderName, out var timestamp) ? timestamp.ToString() : null;
    }

    /// <summary>
    /// Writes a 401 Unauthorized response with a JSON error message.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    private static async Task WriteUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var response = new { error = "Unauthorized", message = "Invalid webhook authentication" };
        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Writes a 500 Internal Server Error response with a JSON error message.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    private static async Task WriteErrorResponse(HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new { error = "Internal Server Error", message = "Webhook authentication error" };
        await context.Response.WriteAsJsonAsync(response);
    }
}
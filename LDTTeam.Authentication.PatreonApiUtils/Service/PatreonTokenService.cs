using System.Text.Json;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Data;
using LDTTeam.Authentication.PatreonApiUtils.Messages;
using LDTTeam.Authentication.PatreonApiUtils.Model.Data;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Wolverine;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

/// <summary>
/// Interface for the Patreon token service, responsible for retrieving and refreshing Patreon access tokens.
/// </summary>
public interface IPatreonTokenService
{
    /// <summary>
    /// Retrieves a valid Patreon access token, refreshing it if necessary.
    /// </summary>
    /// <returns>The current valid access token as a string.</returns>
    Task<string> GetAccessTokenAsync();

    /// <summary>
    /// Forces a reload of the Patreon access token, bypassing the cache.
    /// </summary>
    Task ReloadTokenAsync();
}

/// <summary>
/// Service for managing Patreon OAuth tokens, including caching, refreshing, and notifying other services of token updates.
/// </summary>
/// <remarks>
/// This service checks the in-memory cache, then the database, and refreshes the token from Patreon if needed.
/// It also handles token state, expiry, and notifies other services when a new token is acquired.
/// </remarks>
public partial class PatreonTokenService
    : IPatreonTokenService
{
    private const string CacheKey = "PatreonAccessToken";
    private readonly IMemoryCache _cache;
    private readonly DatabaseContext _databaseContext;
    private readonly IMessageBus _messageBus;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsSnapshot<PatreonConfig> _config;
    private readonly ILogger<PatreonTokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatreonTokenService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache for storing the access token.</param>
    /// <param name="databaseContext">The database context for token persistence.</param>
    /// <param name="messageBus">The message bus for notifying other services.</param>
    /// <param name="httpClientFactory">The HTTP client factory for making requests to Patreon.</param>
    /// <param name="config">The configuration options for Patreon integration.</param>
    /// <param name="logger">The logger instance.</param>
    public PatreonTokenService(
        IMemoryCache cache,
        DatabaseContext databaseContext,
        IMessageBus messageBus,
        IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<PatreonConfig> config,
        ILogger<PatreonTokenService> logger)
    {
        this._cache = cache;
        this._databaseContext = databaseContext;
        this._messageBus = messageBus;
        this._httpClientFactory = httpClientFactory;
        this._config = config;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync()
    {
        //Check local cache first
        if (_cache.TryGetValue<string>(CacheKey, out var cachedKey))
            return cachedKey ?? throw new InvalidDataException("Cached key was null");
        
        //Not in cache, check database
        var tokenInfo = _databaseContext.TokenInformation.FirstOrDefault();
        
        //Should never be null, as we seed it on database creation
        if (tokenInfo == null)
        {
            throw new InvalidDataException("No token information found in database!. Seeding did not work?");
        }
        
        //Check if token is valid and not expiring within 30 minutes
        var plannedExpiry = tokenInfo.ExpiresAt - TimeSpan.FromMinutes(30);
        LogPatreonAccessTokenIsNotCachedOrExpiredCurrentStateStatePlannedRefreshAt(tokenInfo.State, plannedExpiry);

        //If invalid or expiring soon, acquire a new token
        if (tokenInfo.State == State.Invalid || plannedExpiry <= DateTime.UtcNow)
        {
            //Mark as acquiring
            tokenInfo.State = State.Acquiring;
            await _databaseContext.SaveChangesAsync();

            //Use refresh token to get new access token
            var refreshToken = tokenInfo.RefreshToken;
            if (string.IsNullOrWhiteSpace(tokenInfo.RefreshToken))
            {
                LogNoExistingRefreshTokenFoundRequestingInitialTokens();
                refreshToken = _config.Value.InitializingApiRefreshToken;
            }
            else
            {
                LogUsingExistingRefreshTokenToAcquireNewAccessToken();
            }
            
            //Make request to Patreon
            HttpRequestMessage request = new(HttpMethod.Post,
                "https://www.patreon.com/api/oauth2/token" +
                "?grant_type=refresh_token" +
                $"&refresh_token={refreshToken}" +
                $"&client_id={_config.Value.ClientId}" +
                $"&client_secret={_config.Value.ClientSecret}");

            //Send request
            _logger.LogDebug("Requesting new tokens using: " + request.RequestUri);
            var responseMessage = await _httpClientFactory.CreateClient("PatreonTokenClient").SendAsync(request);

            //Check response
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical("Failed to get access token from Patreon: " + responseMessage.StatusCode);
                _logger.LogWarning(await responseMessage.Content.ReadAsStringAsync());
                
                throw new Exception("Failed to get access token from Patreon: " + responseMessage.StatusCode);
            }
            
            //Parse response, expecting JSON in the form of AccessTokenResponse, but with snake_case naming
            var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var response = await responseMessage.Content.ReadFromJsonAsync<AccessTokenResponse>(serializerOptions);
            if (response is null or {RefreshToken: null} or {AccessToken: null} or {ExpiresIn: 0})
            {
                throw new Exception("Refresh, Access Token or Expiry time was null!" + response);
            }

            //Store new tokens in database
            tokenInfo.AccessToken = response.AccessToken;
            tokenInfo.RefreshToken = response.RefreshToken;
            tokenInfo.ExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
            tokenInfo.State = State.Valid;
            await _databaseContext.SaveChangesAsync();
            
            //Notify other services
            await _messageBus.PublishAsync(new PatreonTokenUpdated());
        }

        //If still acquiring in another process, wait for it to finish
        while (tokenInfo.State == State.Acquiring)
        {
            //A different process is acquiring the token, wait for it to finish
            LogWaitingForPatreonTokenToBeAcquiredByAnotherProcess();
            await Task.Delay(1000);
            await _databaseContext.Entry(tokenInfo).ReloadAsync();
        }
        
        //Cache the token for future use
        if (tokenInfo.State == State.Valid)
        {
            cachedKey = tokenInfo.AccessToken;
            var cacheExpiry = tokenInfo.ExpiresAt - TimeSpan.FromMinutes(30);
            _cache.Set(CacheKey, cachedKey, cacheExpiry);
            
            LogAcquiredNewPatreonAccessTokenExpiresAtExpiryRenewalAtRenewalat(tokenInfo.ExpiresAt, cacheExpiry);
        }
        
        //Return the acquired key
        return cachedKey ?? throw new InvalidDataException("Acquired key was null");
    }

    /// <inheritdoc/>
    public async Task ReloadTokenAsync()
    {
        LogReloadingPatreonAccessTokenOnRequest();
        _cache.Remove(CacheKey);
        await GetAccessTokenAsync();
    }

    [LoggerMessage(LogLevel.Warning, "Patreon access token is not cached or expired. Current state: {state}, planned refresh at: {refreshAt}")]
    partial void LogPatreonAccessTokenIsNotCachedOrExpiredCurrentStateStatePlannedRefreshAt(State state, DateTime refreshAt);

    [LoggerMessage(LogLevel.Warning, "No existing refresh token found, requesting initial tokens.")]
    partial void LogNoExistingRefreshTokenFoundRequestingInitialTokens();

    [LoggerMessage(LogLevel.Information, "Using existing refresh token to acquire new access token.")]
    partial void LogUsingExistingRefreshTokenToAcquireNewAccessToken();

    [LoggerMessage(LogLevel.Information, "Waiting for Patreon token to be acquired by another process...")]
    partial void LogWaitingForPatreonTokenToBeAcquiredByAnotherProcess();

    [LoggerMessage(LogLevel.Information, "Acquired new Patreon access token, expires at {expiry}, renewal at: {renewalAt}")]
    partial void LogAcquiredNewPatreonAccessTokenExpiresAtExpiryRenewalAtRenewalat(DateTime expiry, DateTime renewalAt);

    [LoggerMessage(LogLevel.Warning, "Reloading Patreon access token on request.")]
    partial void LogReloadingPatreonAccessTokenOnRequest();
}
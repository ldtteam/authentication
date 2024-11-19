using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNet.Security.OAuth.Minecraft
{
    public class MinecraftAuthenticationHandler : OAuthHandler<MinecraftAuthenticationOptions>
    {
        public MinecraftAuthenticationHandler(
            [NotNull] IOptionsMonitor<MinecraftAuthenticationOptions> options,
            [NotNull] ILoggerFactory logger,
            [NotNull] UrlEncoder encoder,
            [NotNull] ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var scopeParameter = properties.GetParameter<ICollection<string>>(OAuthChallengeProperties.ScopeKey);
            var scope = scopeParameter != null ? FormatScope(scopeParameter) : FormatScope();

            var parameters = new Dictionary<string, string>
            {
                { "client_id", Options.ClientId },
                { "scope", scope },
                { "response_type", "code" },
                { "redirect_uri", redirectUri },
                { "prompt", "select_account" },
            };

            if (Options.UsePkce)
            {
                var bytes = new byte[32];
                RandomNumberGenerator.Fill(bytes);
                var codeVerifier = Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Encode(bytes);

                // Store this for use during the code redemption.
                properties.Items.Add(OAuthConstants.CodeVerifierKey, codeVerifier);

                var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
                var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

                parameters[OAuthConstants.CodeChallengeKey] = codeChallenge;
                parameters[OAuthConstants.CodeChallengeMethodKey] = OAuthConstants.CodeChallengeMethodS256;
            }

            parameters["state"] = Options.StateDataFormat.Protect(properties);

            return QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, parameters!);
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            Dictionary<string, string> tokenRequestParameters = new()
            {
                {"client_id", Options.ClientId},
                {"redirect_uri", context.RedirectUri},
                {"client_secret", Options.ClientSecret},
                {"code", context.Code},
                {"scope", "Xboxlive.signin Xboxlive.offline_access"},
                {"grant_type", "authorization_code"},
            };

            // PKCE https://tools.ietf.org/html/rfc7636#section-4.5, see BuildChallengeUrl
            if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out string? codeVerifier))
            {
                tokenRequestParameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier);
                context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
            }

            FormUrlEncodedContent requestContent = new(tokenRequestParameters);

            HttpRequestMessage requestMessage = new(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            requestMessage.Version = Backchannel.DefaultRequestVersion;
            HttpResponseMessage response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            if (response.IsSuccessStatusCode)
            {
                JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return OAuthTokenResponse.Success(payload);
            }
            else
            {
                string error = "OAuth token endpoint failure: " + response;
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }

        public record XBoxLiveXui(string Uhs);

        public record XBoxLiveDisplayClaims(List<XBoxLiveXui> Xui);

        public record XBoxLiveResponse(string Token, XBoxLiveDisplayClaims DisplayClaims);

        private async Task<XBoxLiveResponse> GetXboxLiveToken(OAuthTokenResponse tokens)
        {
            const string xblTokenEndpoint = "https://user.auth.xboxlive.com/user/authenticate";

            HttpRequestMessage request = new(HttpMethod.Post, xblTokenEndpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");

            Dictionary<object, object> req = new()
            {
                {
                    "Properties",
                    new Dictionary<string, string>
                    {
                        {
                            "AuthMethod", "RPS"
                        },
                        {
                            "SiteName", "user.auth.xboxlive.com"
                        },
                        {
                            "RpsTicket", $"d={tokens.AccessToken}"
                        }
                    }
                },
                {"RelyingParty", "http://auth.xboxlive.com"},
                {"TokenType", "JWT"}
            };

            request.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.Default, "application/json");
            request.Content.Headers.ContentType!.CharSet = "";

            using HttpResponseMessage response = await new HttpClient().SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<XBoxLiveResponse>();

            Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                            "returned a {Status} response with the following payload: {Headers} {Body}.",
                /* Status: */ response.StatusCode,
                /* Headers: */ response.Headers.ToString(),
                /* Body: */ await response.Content.ReadAsStringAsync(Context.RequestAborted));

            throw new HttpRequestException("An error occurred while retrieving the user profile. Most likely your Microsoft account has no minecraft account attached, Please use the Form input instead.");
        }

        public record XstsXui(string Uhs);

        public record XstsDisplayClaims(List<XstsXui> Xui);

        public record XstsResponse(string Token, XstsDisplayClaims DisplayClaims);

        private async Task<XstsResponse> GetXstsToken(XBoxLiveResponse xboxLive)
        {
            const string xstsTokenEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";

            HttpRequestMessage request = new(HttpMethod.Post, xstsTokenEndpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");

            Dictionary<object, object> req = new()
            {
                {
                    "Properties",
                    new Dictionary<string, object>
                    {
                        {
                            "SandboxId", "RETAIL"
                        },
                        {
                            "UserTokens",
                            new List<string>
                            {
                                xboxLive.Token
                            }
                        }
                    }
                },
                {"RelyingParty", "rp://api.minecraftservices.com/"},
                {"TokenType", "JWT"}
            };

            request.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.Default, "application/json");
            request.Content.Headers.ContentType!.CharSet = "";

            using HttpResponseMessage response = await new HttpClient().SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<XstsResponse>();

            Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                            "returned a {Status} response with the following payload: {Headers} {Body}.",
                /* Status: */ response.StatusCode,
                /* Headers: */ response.Headers.ToString(),
                /* Body: */ await response.Content.ReadAsStringAsync(Context.RequestAborted));

            throw new HttpRequestException("An error occurred while retrieving the user profile. Most likely your Microsoft account has no minecraft account attached, Please use the Form input instead.");
        }

        public record MinecraftAuthResponse(string Username, List<string> Roles, string AccessToken, string TokenType)
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; } = AccessToken;
        };

        private async Task<MinecraftAuthResponse> GetMinecraftToken(XBoxLiveResponse xBoxLiveResponse,
            XstsResponse xstsResponse)
        {
            const string minecraftTokenEndpoint = "https://api.minecraftservices.com/authentication/login_with_xbox";

            HttpRequestMessage request = new(HttpMethod.Post, minecraftTokenEndpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

            Dictionary<object, object> req = new()
            {
                {
                    "identityToken",
                    $"XBL3.0 x={xBoxLiveResponse.DisplayClaims.Xui.FirstOrDefault()?.Uhs};{xstsResponse.Token}"
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.Default, "application/json");
            request.Content.Headers.ContentType!.CharSet = "";

            using HttpResponseMessage response = await new HttpClient().SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<MinecraftAuthResponse>();

            Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                            "returned a {Status} response with the following payload: {Headers} {Body}.",
                /* Status: */ response.StatusCode,
                /* Headers: */ response.Headers.ToString(),
                /* Body: */ await response.Content.ReadAsStringAsync(Context.RequestAborted));

            throw new HttpRequestException("An error occurred while retrieving the user profile. Most likely your Microsoft account has no minecraft account attached, Please use the Form input instead.");
        }

        public record MinecraftProfileResponse(string Id, string Name);

        private async Task<MinecraftProfileResponse> GetMinecraftProfile(MinecraftAuthResponse minecraftAuth)
        {
            const string minecraftProfileEndpoint = "https://api.minecraftservices.com/minecraft/profile";

            HttpRequestMessage request = new(HttpMethod.Get, minecraftProfileEndpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {minecraftAuth.AccessToken}");

            using HttpResponseMessage response = await new HttpClient().SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<MinecraftProfileResponse>();

            Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                            "returned a {Status} response with the following payload: {Headers} {Body}.",
                /* Status: */ response.StatusCode,
                /* Headers: */ response.Headers.ToString(),
                /* Body: */ await response.Content.ReadAsStringAsync(Context.RequestAborted));

            throw new HttpRequestException("An error occurred while retrieving the user profile. Most likely your Microsoft account has no minecraft account attached, Please use the Form input instead.");
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            [NotNull] ClaimsIdentity identity,
            [NotNull] AuthenticationProperties properties,
            [NotNull] OAuthTokenResponse tokens)
        {
            XBoxLiveResponse xBoxLiveResponse = await GetXboxLiveToken(tokens);
            XstsResponse xstsResponse = await GetXstsToken(xBoxLiveResponse);
            MinecraftAuthResponse minecraftAuth = await GetMinecraftToken(xBoxLiveResponse, xstsResponse);
            MinecraftProfileResponse minecraftProfile = await GetMinecraftProfile(minecraftAuth);

            using JsonDocument user =
                JsonDocument.Parse(
                    $"{{\"data\":{{\"attributes\":{{\"full_name\": \"{minecraftProfile.Name}\"}}, \"id\": \"{Guid.Parse(minecraftProfile.Id)}\"}}}}");
            OAuthCreatingTicketContext context =
                new(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user.RootElement);
            context.RunClaimActions(user.RootElement.GetProperty("data"));
            
            await Options.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }
    }
}

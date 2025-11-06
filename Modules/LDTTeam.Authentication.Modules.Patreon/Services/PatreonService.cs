using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Patreon.Config;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Modules.Patreon.Services
{
    public class PatreonService(
        IMemoryCache cache,
        IConfiguration configuration,
        PatreonDatabaseContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<PatreonService> logger)
    {

        private const string PatreonAccessTokenCacheKey = "PATREON_ACCESS_TOKEN";

        public class MemberAttributes
        {
            [JsonPropertyName("campaign_lifetime_support_cents")]
            public long LifetimeCents { get; set; }

            [JsonPropertyName("currently_entitled_amount_cents")]
            public long CurrentMonthlyCents { get; set; }
            
            [JsonPropertyName("will_pay_amount_cents")]
            public long WillPayMonthlyCents { get; set; }
            
            [JsonPropertyName("last_charge_status")]
            public string? LastChargeStatus { get; set; }
            
            [JsonPropertyName("last_charge_date")]
            public string? LastChargeDate { get; set; }
            
            [JsonPropertyName("patron_status")]
            public string? PatronStatus { get; set; }
            
            [JsonPropertyName("is_gifted")]
            public bool? IsGifted { get; set; }
        }

        public record UserData(string Id);

        public record RelationshipsUser(UserData Data);

        public record CurrentlyEntitledTiers(List<Tier> Data);
        
        public class MemberRelationships
        {
            [JsonPropertyName("user")] 
            public RelationshipsUser User { get; set; } = null!;
            
            [JsonPropertyName("currently_entitled_tiers")]
            public CurrentlyEntitledTiers CurrentlyEntitledTiers { get; set; } = null!;
        }

        public class Tier
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = null!;
        }

        public record PatreonMember(MemberAttributes Attributes, MemberRelationships Relationships);

        public record PaginationCursors(string? Next);

        public record MetaPagination(PaginationCursors? Cursors);

        public record RequestMeta(MetaPagination Pagination);

        public record PatreonMembersResponse(List<PatreonMember> Data, RequestMeta Meta);

        public async IAsyncEnumerable<PatreonMember> RequestMembers()
        {
            PatreonConfig? patreonConfig = configuration.GetSection("patreon").Get<PatreonConfig>();

            string? cursorNext = null;

            while (true)
            {
                if (patreonConfig == null)
                    throw new Exception("patreon not set in configuration!");

                //// Sample response for (url decoded)
                /// https://www.patreon.com/api/oauth2/v2/campaigns/{campaign_id}/members?
                /// include=currently_entitled_tiers,address&
                /// fields[member]=full_name,is_follower,last_charge_date,last_charge_status,lifetime_support_cents,currently_entitled_amount_cents,patron_status&
                /// fields[tier]=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url&
                /// fields[address]=addressee,city,line_1,line_2,phone_number,postal_code,state
                
                HttpRequestMessage request = new(HttpMethod.Get, 
                    $"https://www.patreon.com/api/oauth2/v2/campaigns/{patreonConfig.CampaignId}/members" +
                    "?include=user,currently_entitled_tiers" +
                    $"&{WebUtility.UrlEncode("page[count]")}=500" +
                    $"&{WebUtility.UrlEncode("fields[member]")}=campaign_lifetime_support_cents,currently_entitled_amount_cents,patron_status,will_pay_amount_cents,last_charge_status,last_charge_date,is_gifted" +
                    $"&{WebUtility.UrlEncode("fields[tier]")}=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url" +
                    cursorNext);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await RequestAccessToken());

                var responseMessage = await httpClientFactory.CreateClient().SendAsync(request);

                if (responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    if (responseMessage.StatusCode != HttpStatusCode.TooManyRequests)
                        throw new Exception("Failed to get members from Patreon: " + responseMessage.StatusCode);
                    
                    logger.LogWarning("Rate limited by Patreon, waiting 1 minute before retrying.");   
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    continue;
                }

                var body = await responseMessage.Content.ReadAsStringAsync();
                //Write the body to a temp file for debugging
                await File.WriteAllTextAsync( DateTime.Now.ToString(CultureInfo.InvariantCulture) + "_patreon_response.json", body);

                var response =
                    JsonSerializer.Deserialize<PatreonMembersResponse>(body);
                
                if (response == null)
                    throw new Exception();

                foreach (PatreonMember member in response.Data)
                {
                    yield return member;
                }
                
                if (response.Meta.Pagination.Cursors?.Next == null)
                    yield break;
                cursorNext = $"&{WebUtility.UrlEncode("page[cursor]")}={response.Meta.Pagination.Cursors?.Next}";
            }
        }

        private class AccessTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
        }

        public async Task<string> RequestAccessToken()
        {
            if (cache.TryGetValue(PatreonAccessTokenCacheKey, out string accessToken))
                return accessToken;

            PatreonConfig? patreonConfig = configuration.GetSection("patreon").Get<PatreonConfig>();

            if (patreonConfig == null)
                throw new Exception("patreon not set in configuration!");

            DbToken? dbToken = await db.Token
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (dbToken == null)
            {
                logger.LogWarning("Patreon access token not found in database. Using initial refresh token: " +
                                  patreonConfig.InitializingApiRefreshToken);
                
                dbToken = new DbToken
                {
                    Id = Guid.NewGuid(),
                    RefreshToken = patreonConfig.InitializingApiRefreshToken
                };
                await db.Token.AddAsync(dbToken);
            }

            HttpRequestMessage request = new(HttpMethod.Post,
                "https://www.patreon.com/api/oauth2/token" +
                "?grant_type=refresh_token" +
                $"&refresh_token={dbToken.RefreshToken}" +
                $"&client_id={patreonConfig.ApiClientId}" +
                $"&client_secret={patreonConfig.ApiClientSecret}");

            logger.LogWarning("Requesting new tokens using: " + request.RequestUri);
            HttpResponseMessage responseMessage = await httpClientFactory.CreateClient().SendAsync(request);

            if (!responseMessage.IsSuccessStatusCode)
            {
                logger.LogCritical("Failed to get access token from Patreon: " + responseMessage.StatusCode);
                logger.LogWarning(await responseMessage.Content.ReadAsStringAsync());
                
                throw new Exception("Failed to get access token from Patreon: " + responseMessage.StatusCode);
            }
            
            AccessTokenResponse? response = await responseMessage.Content.ReadFromJsonAsync<AccessTokenResponse>();

            if (response is {RefreshToken: null} or {AccessToken: null})
            {
                throw new Exception("Refresh or Access token was null!" + response);
            }

            dbToken.RefreshToken = response!.RefreshToken!; // just throw an exception if it fails

            await db.SaveChangesAsync();

            MemoryCacheEntryOptions cacheExpiryOptions = new()
            {
                AbsoluteExpiration = DateTime.Now.AddHours(1),
                Priority = CacheItemPriority.High
            };

            cache.Set(PatreonAccessTokenCacheKey, response.AccessToken, cacheExpiryOptions);

            return response.AccessToken!;
        }
    }
}

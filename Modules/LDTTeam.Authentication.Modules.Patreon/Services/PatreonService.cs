using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Patreon.Config;
using LDTTeam.Authentication.Modules.Patreon.Data;
using LDTTeam.Authentication.Modules.Patreon.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace LDTTeam.Authentication.Modules.Patreon.Services
{
    public class PatreonService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PatreonDatabaseContext _db;

        private const string PatreonAccessTokenCacheKey = "PATREON_ACCESS_TOKEN";

        public PatreonService(IMemoryCache cache, IConfiguration configuration, PatreonDatabaseContext db,
            IHttpClientFactory httpClientFactory)
        {
            _cache = cache;
            _configuration = configuration;
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public class MemberAttributes
        {
            [JsonPropertyName("campaign_lifetime_support_cents")]
            public long LifetimeCents { get; set; }

            [JsonPropertyName("currently_entitled_amount_cents")]
            public long CurrentMonthlyCents { get; set; }
        }

        public record UserData(string Id);

        public record RelationshipsUser(UserData Data);

        public record MemberRelationships(RelationshipsUser User);

        public record PatreonMember(MemberAttributes Attributes, MemberRelationships Relationships);

        public record PaginationCursors(string? Next);

        public record MetaPagination(PaginationCursors? Cursors);

        public record RequestMeta(MetaPagination Pagination);

        public record PatreonMembersResponse(List<PatreonMember> Data, RequestMeta Meta);

        public async IAsyncEnumerable<PatreonMember> RequestMembers()
        {
            PatreonConfig? patreonConfig = _configuration.GetSection("patreon").Get<PatreonConfig>();

            string? cursorNext = null;

            while (true)
            {
                if (patreonConfig == null)
                    throw new Exception("patreon not set in configuration!");

                HttpRequestMessage request = new(HttpMethod.Get, 
                    $"https://www.patreon.com/api/oauth2/v2/campaigns/{patreonConfig.CampaignId}/members" +
                    "?include=user" +
                    $"&{WebUtility.UrlEncode("fields[member]")}=campaign_lifetime_support_cents,currently_entitled_amount_cents,patron_status" + 
                    cursorNext);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await RequestAccessToken());

                HttpResponseMessage responseMessage = await _httpClientFactory.CreateClient().SendAsync(request);
                PatreonMembersResponse? response =
                    await responseMessage.Content.ReadFromJsonAsync<PatreonMembersResponse>();
                
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
            if (_cache.TryGetValue(PatreonAccessTokenCacheKey, out string accessToken))
                return accessToken;

            PatreonConfig? patreonConfig = _configuration.GetSection("patreon").Get<PatreonConfig>();

            if (patreonConfig == null)
                throw new Exception("patreon not set in configuration!");

            DbToken? dbToken = await _db.Token
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (dbToken == null)
            {
                dbToken = new DbToken
                {
                    Id = Guid.NewGuid(),
                    RefreshToken = patreonConfig.InitializingApiRefreshToken
                };
                await _db.Token.AddAsync(dbToken);
            }

            HttpRequestMessage request = new(HttpMethod.Post,
                "https://www.patreon.com/api/oauth2/token" +
                "?grant_type=refresh_token" +
                $"&refresh_token={dbToken.RefreshToken}" +
                $"&client_id={patreonConfig.ApiClientId}" +
                $"&client_secret={patreonConfig.ApiClientSecret}");

            HttpResponseMessage responseMessage = await _httpClientFactory.CreateClient().SendAsync(request);

            AccessTokenResponse? response = await responseMessage.Content.ReadFromJsonAsync<AccessTokenResponse>();

            if (response is {RefreshToken: null} or {AccessToken: null})
            {
                throw new Exception("Refresh or Access token was null!", response);
            }

            dbToken.RefreshToken = response!.RefreshToken!; // just throw an exception if it fails

            await _db.SaveChangesAsync();

            MemoryCacheEntryOptions cacheExpiryOptions = new()
            {
                AbsoluteExpiration = DateTime.Now.AddHours(1),
                Priority = CacheItemPriority.High
            };

            _cache.Set(PatreonAccessTokenCacheKey, response.AccessToken, cacheExpiryOptions);

            return response.AccessToken!;
        }
    }
}

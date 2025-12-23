using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using JasperFx.Core;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Model.App;
using LDTTeam.Authentication.PatreonApiUtils.Model.Requests;
using Microsoft.Extensions.Options;

namespace LDTTeam.Authentication.PatreonApiUtils.Service;

public interface IPatreonDataService
{
    Task<PatreonContribution?> GetFor(Guid patreonId);

    IAsyncEnumerable<PatreonContribution> All();
}

public class PatreonDataService(
    IPatreonTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IOptionsSnapshot<PatreonConfig> config,
    ILogger<PatreonDataService> logger) : IPatreonDataService
{
    public async Task<PatreonContribution?> GetFor(Guid memberId)
    {
        while (true)
        {
            // Sample response for (url decoded)
            // https://www.patreon.com/api/oauth2/v2/members/{id}?
            // include=currently_entitled_tiers,address&
            // fields[member]=full_name,is_follower,last_charge_date,last_charge_status,lifetime_support_cents,currently_entitled_amount_cents,patron_status&
            // fields[tier]=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url&
            // fields[address]=addressee,city,line_1,line_2,phone_number,postal_code,state

            HttpRequestMessage request = new(HttpMethod.Get,
                $"https://www.patreon.com/api/oauth2/v2/members/{memberId}" +
                "?include=user,currently_entitled_tiers,campaign" +
                $"&{WebUtility.UrlEncode("fields[member]")}=campaign_lifetime_support_cents,currently_entitled_amount_cents,patron_status,will_pay_amount_cents,last_charge_status,last_charge_date,is_gifted" +
                $"&{WebUtility.UrlEncode("fields[tier]")}=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url");
            
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await tokenService.GetAccessTokenAsync());

            var responseMessage = await httpClientFactory.CreateClient("PatreonApiClient").SendAsync(request);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                if (responseMessage.StatusCode != HttpStatusCode.TooManyRequests)
                    throw new Exception("Failed to get members from Patreon: " + responseMessage.StatusCode);

                logger.LogWarning("Rate limited by Patreon, waiting 1 minute before retrying.");
                await Task.Delay(TimeSpan.FromMinutes(1));
                continue;
            }

            var body = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var response = JsonSerializer.Deserialize<CampaignMemberResponse?>(body, options);

            if (response == null)
            {
                logger.LogCritical("Failed to deserialize Patreon member response: {Body}", body);
                return null;
            }

            var tiers = ExtractIncludedTiers(response);

            return MapMemberInformation(response.Data, tiers);
        }
    }

    public async IAsyncEnumerable<PatreonContribution> All()
    {
        var patreonConfig = config.Value;

        string? cursorNext = null;

        while (true)
        {
            if (patreonConfig == null)
                throw new Exception("patreon not set in configuration!");

            // Sample response for (url decoded)
            // https://www.patreon.com/api/oauth2/v2/campaigns/{campaign_id}/members?
            // include=currently_entitled_tiers,address&
            // fields[member]=full_name,is_follower,last_charge_date,last_charge_status,lifetime_support_cents,currently_entitled_amount_cents,patron_status&
            // fields[tier]=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url&
            // fields[address]=addressee,city,line_1,line_2,phone_number,postal_code,state

            HttpRequestMessage request = new(HttpMethod.Get,
                $"https://www.patreon.com/api/oauth2/v2/campaigns/{patreonConfig.CampaignId}/members" +
                "?include=user,currently_entitled_tiers" +
                $"&{WebUtility.UrlEncode("page[count]")}=500" +
                $"&{WebUtility.UrlEncode("fields[member]")}=campaign_lifetime_support_cents,currently_entitled_amount_cents,patron_status,will_pay_amount_cents,last_charge_status,last_charge_date,is_gifted" +
                $"&{WebUtility.UrlEncode("fields[tier]")}=amount_cents,created_at,description,discord_role_ids,edited_at,patron_count,published,published_at,requires_shipping,title,url" +
                cursorNext);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", await tokenService.GetAccessTokenAsync());

            var responseMessage = await httpClientFactory.CreateClient("PatreonApiClient").SendAsync(request);

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                if (responseMessage.StatusCode != HttpStatusCode.TooManyRequests)
                    throw new Exception("Failed to get members from Patreon: " + responseMessage.StatusCode);

                logger.LogWarning("Rate limited by Patreon, waiting 1 minute before retrying.");
                await Task.Delay(TimeSpan.FromMinutes(1));
                continue;
            }

            var body = await responseMessage.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var response = JsonSerializer.Deserialize<CampaignMembersResponse?>(body, options);

            if (response == null)
                throw new Exception();

            var tiers = ExtractIncludedTiers(response);

            foreach (var member in response.Data)
            {
                yield return MapMemberInformation(member, tiers);
            }

            if (response.Meta?.Pagination.Cursors?.Next == null)
                yield break;
            cursorNext = $"&{WebUtility.UrlEncode("page[cursor]")}={response.Meta?.Pagination.Cursors.Next}";
        }
    }

    private static Dictionary<IncludedDataReference, string> ExtractIncludedTiers(PatreonResponseBase response)
    {
        Dictionary<IncludedDataReference, string> tiers = new();
        foreach (var included in response.Included.Where(included =>
                     string.Equals(included.Type, "tier", StringComparison.OrdinalIgnoreCase)))
        {
            tiers[new IncludedDataReference
            {
                Type = included.Type,
                Id = included.Id
            }] = included.Attributes["title"].ToString() ?? "";
        }

        return tiers;
    }

    private PatreonContribution MapMemberInformation(Member member, Dictionary<IncludedDataReference, string> tiers)
    {
        var isActive = member.Attributes.PatronStatus == "active_patron";
        var hasPaid = member.Attributes.LastChargeStatus?.EqualsIgnoreCase("paid") ?? false;
        
        var shouldHaveTiers = isActive && hasPaid;

        return new PatreonContribution
        {
            PatreonId = member.Relationships.User?.Data?.Id,
            MembershipId = Guid.Parse(member.Id),
            LifetimeCents = member.Attributes.CampaignLifetimeSupportCents,
            IsGifted = member.Attributes.IsGifted,
            Tiers = shouldHaveTiers
                ? member.Relationships.CurrentlyEntitledTiers.Data
                    .Select(tierRef => tiers.GetValueOrDefault(tierRef, "Unknown Tier"))
                    .ToList()
                : Array.Empty<string>(),
            LastChargeDate =
                member.Attributes.LastChargeDate == null ? null :
                DateTime.Parse(member.Attributes.LastChargeDate, null, System.Globalization.DateTimeStyles.RoundtripKind),
            LastChargeSuccessful = hasPaid,
            CampaignId = int.Parse(member.Relationships.Campaign?.Data?.Id ?? "-1")
        };
    }
}
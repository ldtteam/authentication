using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Api.Utils;
using LDTTeam.Authentication.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable SpecifyStringComparison

namespace LDTTeam.Authentication.Server.Api
{
    [ApiController]
    [Route("Api")]
    public class ApiController : ControllerBase
    {
        private readonly IServiceProvider _services;
        private readonly DatabaseContext _db;
        private readonly IBackgroundEventsQueue _eventsQueue;

        public ApiController(IServiceProvider services, DatabaseContext db, IBackgroundEventsQueue eventsQueue)
        {
            _services = services;
            _db = db;
            _eventsQueue = eventsQueue;
        }

        [HttpGet("webhook/{provider}")]
        public async Task<ActionResult> WebhookEndpoint(string provider)
        {
            await _eventsQueue.QueueBackgroundWorkItemAsync((events, scope, _) =>
                events._refreshContentEvent.InvokeAsync(scope, new List<string> {provider}));
            return Ok();
        }

        [HttpGet("{provider}/{providerKey}/{rewardId}")]
        public async Task<ActionResult<bool>> Endpoint(string provider, string providerKey, string rewardId)
        {
            IdentityUserLogin<string>? loginInfo = await _db.UserLogins.FirstOrDefaultAsync(x =>
                x.LoginProvider.ToLower() == provider.ToLower() &&
                x.ProviderKey.ToLower() == providerKey.ToLower());

            string? userId = loginInfo?.UserId;

            if (userId == null)
            {
                if (provider.ToLower() != "minecraft")
                    return NotFound();

                IdentityUserClaim<string>? claim = await _db.UserClaims
                    .FirstOrDefaultAsync(x =>
                        x.ClaimType == "urn:minecraft:user:id" && x.ClaimValue.ToLower() == providerKey.ToLower());

                if (claim == null)
                    return NotFound();

                userId = claim.UserId;
            }

            Reward? reward = await _db.Rewards
                .Include(x => x.Conditions)
                .FirstOrDefaultAsync(x => x.Id == rewardId);

            if (reward == null)
                return NotFound();

            CancellationTokenSource source = new();
            using IServiceScope scope = _services.CreateScope();

            foreach (ConditionInstance conditionInstance in reward.Conditions)
            {
                ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                    x.ModuleName == conditionInstance.ModuleName &&
                    x.Name == conditionInstance.ConditionName);

                if (condition == null) continue;

                if (!await condition.ExecuteAsync(scope, conditionInstance,
                    userId,
                    source.Token)) continue;

                return true;
            }

            return false;
        }

        [HttpGet("test")]
        public async Task<ActionResult<Dictionary<string, bool>>> Test()
        {
            Dictionary<string, bool> results = new();

            ApplicationUser? user = await _db.Users.FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            CancellationTokenSource source = new();
            using IServiceScope scope = _services.CreateScope();
            await foreach (Reward reward in _db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable()
                .WithCancellation(source.Token))
            {
                results[reward.Id] = false;
                foreach (ConditionInstance conditionInstance in reward.Conditions)
                {
                    ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                        x.ModuleName == conditionInstance.ModuleName &&
                        x.Name == conditionInstance.ConditionName);

                    if (condition == null) continue;

                    if (!await condition.ExecuteAsync(scope, conditionInstance,
                        user.Id,
                        source.Token)) continue;

                    results[reward.Id] = true;
                    break;
                }
            }

            return results;
        }
    }
}
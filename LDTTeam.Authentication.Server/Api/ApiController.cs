using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Api.Utils;
using LDTTeam.Authentication.Server.Data;
using LDTTeam.Authentication.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ReSharper disable SpecifyStringComparison

namespace LDTTeam.Authentication.Server.Api
{
    [ApiController]
    [Route("Api")]
    public class ApiController(
        IBackgroundEventsQueue eventsQueue,
        IConditionService conditionService,
        DatabaseContext context)
        : ControllerBase
    {
        [HttpGet("webhook/{provider}")]
        [HttpPost("webhook/{provider}")]
        public async Task<ActionResult> WebhookEndpoint(string provider, CancellationToken token)
        {
            if (provider == "all")
            {
                await eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
                {
                    await events._refreshContentEvent.InvokeAsync(scope, null);
                    await events._postRefreshContentEvent.InvokeAsync(scope);
                }, token);
                return Ok();
            }

            await eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope, [provider]);
                await events._postRefreshContentEvent.InvokeAsync(scope);
            }, token);
            return Ok();
        }

        [HttpGet("{provider}/{providerKey}/{rewardId}")]
        public async Task<ActionResult<bool>> Endpoint(string provider, string providerKey, string rewardId, CancellationToken token)
        {
            bool? check = await conditionService.CheckReward(provider, providerKey, rewardId, token);

            if (check == null)
                return NotFound();
            
            try
            {
                EndpointMetric? metric = await context.Metrics.FirstOrDefaultAsync(x =>
                    x.Provider.ToLower() == provider.ToLower() &&
                    x.RewardId.ToLower() == rewardId.ToLower() &&
                    x.Result == check, cancellationToken: token);

                if (metric == null)
                {
                    metric = new EndpointMetric
                    {
                        Provider = provider.ToLower(),
                        RewardId = rewardId.ToLower(),
                        Result = check.Value,
                        Count = 0
                    };

                    await context.AddAsync(metric, token);
                }
                metric.Count++;

                await context.SaveChangesAsync(token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Metrics Failed: {e}");
            }

            return check;
        }

        [HttpGet("/metrics")]
        public async Task<ActionResult<List<EndpointMetric>>> Metrics(CancellationToken token)
        {
            return await context.Metrics
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken: token);
        }
 
        [HttpGet("/metrics/{id:guid}")]
        public async Task<ActionResult<List<HistoricalEndpointMetric>>> HistoricalMetrics(Guid id, bool? loadAll, CancellationToken token)
        {
            var all = loadAll ?? false;
            
            IQueryable<HistoricalEndpointMetric> query = context.HistoricalMetrics
                .Where(x => x.Metric.Id == id);

            DateTimeOffset sevenDaysAgo = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
            
            if (!all)
                query = query.Where(x => x.DateTime > sevenDaysAgo);
            
            return await query.ToListAsync(cancellationToken: token);
        }
    }
}
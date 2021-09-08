using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ApiController : ControllerBase
    {
        private readonly IBackgroundEventsQueue _eventsQueue;
        private readonly IConditionService _conditionService;
        private readonly DatabaseContext _context;

        public ApiController(IBackgroundEventsQueue eventsQueue, IConditionService conditionService,
            DatabaseContext context)
        {
            _eventsQueue = eventsQueue;
            _conditionService = conditionService;
            _context = context;
        }

        [HttpGet("webhook/{provider}")]
        [HttpPost("webhook/{provider}")]
        public async Task<ActionResult> WebhookEndpoint(string provider)
        {
            if (provider == "all")
            {
                await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
                {
                    await events._refreshContentEvent.InvokeAsync(scope, null);
                    await events._postRefreshContentEvent.InvokeAsync(scope);
                });
                return Ok();
            }

            await _eventsQueue.QueueBackgroundWorkItemAsync(async (events, scope, _) =>
            {
                await events._refreshContentEvent.InvokeAsync(scope, new List<string> {provider});
                await events._postRefreshContentEvent.InvokeAsync(scope);
            });
            return Ok();
        }

        [HttpGet("{provider}/{providerKey}/{rewardId}")]
        public async Task<ActionResult<bool>> Endpoint(string provider, string providerKey, string rewardId)
        {
            bool? check = await _conditionService.CheckReward(provider, providerKey, rewardId);

            if (check == null)
                return NotFound();
            
            try
            {
                EndpointMetric? metric = await _context.Metrics.FirstOrDefaultAsync(x =>
                    x.Provider.ToLower() == provider.ToLower() &&
                    x.RewardId.ToLower() == rewardId.ToLower() &&
                    x.Result == check
                );

                if (metric == null)
                {
                    metric = new EndpointMetric
                    {
                        Provider = provider.ToLower(),
                        RewardId = rewardId.ToLower(),
                        Result = check.Value,
                        Count = 0
                    };

                    await _context.AddAsync(metric);
                }
                metric.Count++;

                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Metrics Failed: {e}");
            }

            return check;
        }

        [HttpGet("/metrics")]
        public async Task<ActionResult<List<EndpointMetric>>> Metrics()
        {
            return await _context.Metrics
                .OrderBy(x => x.Id)
                .ToListAsync();
        }
 
        [HttpGet("/metrics/{id:guid}")]
        public async Task<ActionResult<List<HistoricalEndpointMetric>>> HistoricalMetrics(Guid id, bool all = false)
        {
            IQueryable<HistoricalEndpointMetric> query = _context.HistoricalMetrics
                .Where(x => x.Metric.Id == id);

            DateTimeOffset sevenDaysAgo = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(7));
            
            if (!all)
                query = query.Where(x => x.DateTime > sevenDaysAgo);
            
            return await query.ToListAsync();
        }
    }
}
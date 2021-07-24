using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api.Events;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Modules.Api.Utils;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable SpecifyStringComparison

namespace LDTTeam.Authentication.Server.Api
{
    [ApiController]
    [Route("Api")]
    public class ApiController : ControllerBase
    {
        private readonly IBackgroundEventsQueue _eventsQueue;
        private readonly IConditionService _conditionService;

        public ApiController(IBackgroundEventsQueue eventsQueue, IConditionService conditionService)
        {
            _eventsQueue = eventsQueue;
            _conditionService = conditionService;
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
            return await _conditionService.CheckReward(provider, providerKey, rewardId);
        }
    }
}

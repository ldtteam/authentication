using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using LDTTeam.Authentication.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Server.Pages.Account.Manage
{
    [Authorize]
    public class Rewards : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceProvider _services;
        private readonly DatabaseContext _db;

        public Rewards(UserManager<ApplicationUser> userManager, IServiceProvider services, DatabaseContext db)
        {
            _userManager = userManager;
            _services = services;
            _db = db;
        }

        public readonly Dictionary<string, bool> RewardsDictionary = new();

        public async Task<ActionResult> OnGetAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            CancellationTokenSource source = new();
            using IServiceScope scope = _services.CreateScope();
            await foreach (Reward reward in _db.Rewards.Include(x => x.Conditions).AsAsyncEnumerable()
                .WithCancellation(source.Token))
            {
                RewardsDictionary[reward.Id] = false;
                foreach (ConditionInstance conditionInstance in reward.Conditions)
                {
                    ICondition? condition = Conditions.Registry.FirstOrDefault(x =>
                        x.ModuleName == conditionInstance.ModuleName &&
                        x.Name == conditionInstance.ConditionName);

                    if (condition == null) continue;

                    if (!await condition.ExecuteAsync(scope, conditionInstance,
                        user.Id,
                        source.Token)) continue;

                    RewardsDictionary[reward.Id] = true;
                    break;
                }
            }

            return Page();
        }
    }
}
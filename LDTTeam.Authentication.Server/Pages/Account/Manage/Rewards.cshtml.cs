using System.Collections.Generic;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Api.Rewards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LDTTeam.Authentication.Server.Pages.Account.Manage
{
    [Authorize]
    public class Rewards : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConditionService _conditionService;

        public Rewards(UserManager<ApplicationUser> userManager, IConditionService conditionService)
        {
            _userManager = userManager;
            _conditionService = conditionService;
        }

        public Dictionary<string, bool> RewardsDictionary { get; set; } = null!;

        public async Task<ActionResult> OnGetAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            RewardsDictionary = await _conditionService.GetRewardsForUser(user.Id);

            return Page();
        }
    }
}
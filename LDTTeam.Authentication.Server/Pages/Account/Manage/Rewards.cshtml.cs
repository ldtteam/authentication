using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Server.Services;
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
        private readonly IAssignedRewardRepository _assignedRewardRepository;
        private readonly IRewardRepository _rewardRepository;

        public Rewards(UserManager<ApplicationUser> userManager, IAssignedRewardRepository assignedRewardRepository, IRewardRepository rewardRepository)
        {
            _userManager = userManager;
            _assignedRewardRepository = assignedRewardRepository;
            _rewardRepository = rewardRepository;
        }

        public Dictionary<string, bool> RewardsDictionary { get; set; } = null!;

        public async Task<ActionResult> OnGetAsync(CancellationToken token)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var assignedRewards = (await _assignedRewardRepository.GetForUserAsync(user.Id, token))
                .Select(r => r.Type + "-" + r.Reward)
                .ToHashSet();
            
            var rewards = await _rewardRepository.GetAllAsync(token);

            
            RewardsDictionary = new Dictionary<string, bool>();
            foreach (var assignedReward in rewards)
            {
                var key = assignedReward.Type + "-" + assignedReward.Name;
                RewardsDictionary[key] = assignedRewards.Contains(key);
            }

            return Page();
        }
    }
}
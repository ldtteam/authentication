using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public class Reward
    {
        public string Id { get; set; }
        
        public List<ConditionInstance> Conditions { get; set; }
    }
}
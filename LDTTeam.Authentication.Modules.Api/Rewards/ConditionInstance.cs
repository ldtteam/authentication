namespace LDTTeam.Authentication.Modules.Api.Rewards
{
    public class ConditionInstance
    {
        public Reward Reward { get; set; }
        
        public string RewardId { get; set; }
        
        public string ModuleName { get; set; }
        
        public string ConditionName { get; set; }
        
        public string LambdaString { get; set; }
    }
}
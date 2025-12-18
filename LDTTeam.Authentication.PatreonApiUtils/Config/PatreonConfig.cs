namespace LDTTeam.Authentication.PatreonApiUtils.Config
{
    public class PatreonConfig
    {
        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }
        
        public string InitializingApiRefreshToken { get; set; }
        
        public int CampaignId { get; set; }
    }
}
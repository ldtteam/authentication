namespace LDTTeam.Authentication.Modules.Patreon.Config
{
    public class PatreonConfig
    {
        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }
        
        public string ApiClientId { get; set; }
        
        public string ApiClientSecret { get; set; }
        
        public string InitializingApiRefreshToken { get; set; }
        
        public int CampaignId { get; set; }
    }
}
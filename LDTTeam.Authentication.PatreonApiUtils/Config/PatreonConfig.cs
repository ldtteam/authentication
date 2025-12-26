namespace LDTTeam.Authentication.PatreonApiUtils.Config
{
    public class PatreonConfig
    {
        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }
        
        public string InitializingApiRefreshToken { get; set; }
        
        public int CampaignId { get; set; }
        
        public List<string> Tiers { get; set; } = new()
        {
            "Token",
            "Settler",
            "Citizen",
            "Noble",
            "High Noble",
            "Aristocrat",
            "Mod Sponsor",
            "Bank Roll"
        };
    }
}
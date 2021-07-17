namespace LDTTeam.Authentication.Modules.Patreon.Data.Models
{
    public class DbPatreonMember
    {
        public string Id { get; set; } = null!;
        
        public long Lifetime { get; set; }
        public long Monthly { get; set; }

        public DbPatreonMember(string id, long lifetime, long monthly)
        {
            Id = id;
            Lifetime = lifetime;
            Monthly = monthly;
        }
    }
}
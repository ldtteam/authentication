namespace LDTTeam.Authentication.Modules.GitHub.Data.Models
{
    public class DbGithubTeamUser
    {
        public DbGitHubUser? User { get; set; }
        public int UserId { get; set; }
        
        public DbGitHubTeam? Team { get; set; }
        public int TeamId { get; set; }

        public DbGithubTeamUser(int userId, int teamId)
        {
            UserId = userId;
            TeamId = teamId;
        }
    }
}
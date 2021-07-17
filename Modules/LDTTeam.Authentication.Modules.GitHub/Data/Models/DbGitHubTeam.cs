using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.GitHub.Data.Models
{
    public class DbGitHubTeam
    {
        public int Id { get; set; }

        public string Slug { get; set; }
        
        public List<DbGithubTeamUser> UserRelationships { get; set; } = new();

        public DbGitHubTeam(int id, string slug)
        {
            Id = id;
            Slug = slug;
        }
    }
}
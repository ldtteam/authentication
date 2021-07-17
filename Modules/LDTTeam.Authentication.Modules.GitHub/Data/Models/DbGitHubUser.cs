using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.GitHub.Data.Models
{
    public class DbGitHubUser
    {
        public int Id { get; set; }

        public List<DbGithubTeamUser> TeamRelationships { get; set; } = new();

        public DbGitHubUser(int id)
        {
            Id = id;
        }
    }
}
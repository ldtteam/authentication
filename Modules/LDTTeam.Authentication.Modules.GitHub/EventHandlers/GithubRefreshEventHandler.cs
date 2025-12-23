using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.GitHub.Config;
using LDTTeam.Authentication.Modules.GitHub.Data;
using LDTTeam.Authentication.Modules.GitHub.Data.Models;
using LDTTeam.Authentication.Modules.GitHub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using Team = Octokit.Team;
using User = Octokit.User;

namespace LDTTeam.Authentication.Modules.GitHub.EventHandlers
{
    public class GithubRefreshEventHandler
    {
        private readonly GitHubService _github;
        private readonly GitHubDatabaseContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GithubRefreshEventHandler> _logger;

        public GithubRefreshEventHandler(GitHubService github, GitHubDatabaseContext db, IConfiguration configuration,
            ILogger<GithubRefreshEventHandler> logger)
        {
            _github = github;
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogDebug("Github Refresh Started");

            GitHubConfig? githubConfig = _configuration.GetSection("github").Get<GitHubConfig>();

            if (githubConfig == null)
                throw new Exception("github not set in configuration!");

            GitHubClient gitHubClient = await _github.GetInstallationClient();
            IReadOnlyList<Team> teams = await gitHubClient.Organization.Team.GetAll(githubConfig.Organisation);

            IReadOnlyList<DbGitHubTeam> dbTeams = await _db.Teams.ToListAsync();

            foreach (DbGitHubTeam dbTeam in dbTeams)
            {
                if (teams.Any(x => x.Id == dbTeam.Id)) continue;

                // team deleted from github
                _db.Teams.Remove(dbTeam);
                _logger.LogDebug($"GitHub team removed: {dbTeam.Slug}");
            }

            foreach (Team team in teams)
            {
                if (dbTeams.Any(x => x.Id == team.Id))
                    continue; // team already sync with db

                // team added to github
                _db.Teams.Add(new DbGitHubTeam((int) team.Id, team.Slug));
                _logger.LogDebug($"GitHub team added: {team.Slug}");
            }

            await _db.SaveChangesAsync();

            foreach (DbGitHubTeam dbTeam in await _db.Teams
                .Include(x => x.UserRelationships)
                .ThenInclude(x => x.User)
                .ToListAsync())
            {
                IReadOnlyList<User> users = await gitHubClient.Organization.Team.GetAllMembers(dbTeam.Id);

                IReadOnlyList<DbGitHubUser> dbUsers = await _db.Users.ToListAsync();

                foreach (User user in users.Where(x => dbUsers.All(y => y.Id != x.Id)))
                {
                    await _db.Users.AddAsync(new DbGitHubUser((int) user.Id));
                }

                await _db.SaveChangesAsync();

                foreach (DbGithubTeamUser teamRelationship in dbTeam.UserRelationships.Where(teamRelationship =>
                    users.All(x => x.Id != teamRelationship.UserId)).ToList())
                {
                    dbTeam.UserRelationships.Remove(teamRelationship); // user deleted from team
                    _logger.LogDebug($"GitHub user {teamRelationship.UserId} removed from team {dbTeam.Slug}");
                }

                foreach (User user in users)
                {
                    if (dbTeam.UserRelationships.Any(x => x.UserId == user.Id))
                        continue; // user already synced with db

                    // user added to team
                    dbTeam.UserRelationships.Add(new DbGithubTeamUser((int) user.Id,
                        dbTeam.Id));
                    _logger.LogDebug($"GitHub user {user.Login} added to team {dbTeam.Slug}");
                }
            }

            await _db.SaveChangesAsync();

            _logger.LogDebug("Github Refresh Finished");
        }
    }
}
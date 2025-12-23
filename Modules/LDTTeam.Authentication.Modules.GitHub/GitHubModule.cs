using System;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.GitHub.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.GitHub
{
    public class GitHubModule : IModule
    {
        public string ModuleName => "GitHub";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
        {
            GitHubConfig? githubConfig = configuration.GetSection("github").Get<GitHubConfig>();

            if (githubConfig == null)
                throw new Exception("github not set in configuration!");
            
            return builder.AddGitHub(o =>
            {
                o.ClientId = githubConfig.ClientId;
                o.ClientSecret = githubConfig.ClientSecret;

                o.SaveTokens = true;
            });
        }
    }
}
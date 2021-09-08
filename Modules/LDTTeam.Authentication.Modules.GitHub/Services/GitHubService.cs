using System;
using System.IO;
using System.Threading.Tasks;
using GitHubJwt;
using LDTTeam.Authentication.Modules.GitHub.Config;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Octokit;

namespace LDTTeam.Authentication.Modules.GitHub.Services
{
    public class GitHubService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _cache;

        private const string InstallationTokenCacheKey = "GITHUB_INSTALLATION_TOKEN";
        private const string InstallationClientCacheKey = "GITHUB_INSTALLATION_CLIENT";

        public GitHubService(IWebHostEnvironment environment, IMemoryCache cache, IConfiguration configuration)
        {
            _environment = environment;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<GitHubClient> GetInstallationClient()
        {
            if (_cache.TryGetValue(InstallationClientCacheKey, out GitHubClient client))
                return client;

            GitHubClient newClient = new(new ProductHeaderValue("LDTTeam"))
            {
                Credentials = new Credentials(await GetInstallationToken()),
            };

            MemoryCacheEntryOptions cacheExpiryOptions = new()
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(30),
                Priority = CacheItemPriority.High
            };

            _cache.Set(InstallationClientCacheKey, newClient, cacheExpiryOptions);
            return newClient;
        }

        private async Task<string> GetInstallationToken()
        {
            if (_cache.TryGetValue(InstallationTokenCacheKey, out string accessToken))
                return accessToken;

            string jwt = CreateTokenRequestToken();

            GitHubClient appClient = new(new ProductHeaderValue("LDTTeam"))
            {
                Credentials = new Credentials(jwt, AuthenticationType.Bearer)
            };

            GitHubConfig? githubConfig = _configuration.GetSection("github").Get<GitHubConfig>();

            if (githubConfig == null)
                throw new Exception("github not set in configuration!");

            Installation installation =
                await appClient.GitHubApps.GetOrganizationInstallationForCurrent(githubConfig.Organisation);

            if (installation == null)
                throw new Exception($"github installation for org {githubConfig.Organisation} not found");

            AccessToken token = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

            MemoryCacheEntryOptions cacheExpiryOptions = new()
            {
                AbsoluteExpiration = token.ExpiresAt.Subtract(TimeSpan.FromMinutes(10)),
                Priority = CacheItemPriority.High
            };

            _cache.Set(InstallationTokenCacheKey, token.Token, cacheExpiryOptions);

            return token.Token;
        }

        private string CreateTokenRequestToken()
        {
            string privateKey = _environment.IsDevelopment() && File.Exists("privateKey.Development.pem")
                ? "privateKey.Development.pem"
                : "privateKey.pem";

            GitHubConfig? githubConfig = _configuration.GetSection("github").Get<GitHubConfig>();

            if (githubConfig == null)
                throw new Exception("github not set in configuration!");

            GitHubJwtFactory generator = new(
                new FilePrivateKeySource(privateKey),
                new GitHubJwtFactoryOptions
                {
                    AppIntegrationId = githubConfig.ApplicationId, // The GitHub App Id
                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                }
            );

            return generator.CreateEncodedJwtToken();
        }
    }
}
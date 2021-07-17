using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.GitHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.GitHub.Services
{
    public class GitHubDatabaseMigrationTask : IStartupTask
    {
        private readonly IServiceProvider _services;

        public GitHubDatabaseMigrationTask(IServiceProvider services)
        {
            _services = services;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            using IServiceScope serviceScope =
                _services.GetService<IServiceScopeFactory>()!.CreateScope();
            GitHubDatabaseContext dbContext =
                serviceScope.ServiceProvider.GetRequiredService<GitHubDatabaseContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
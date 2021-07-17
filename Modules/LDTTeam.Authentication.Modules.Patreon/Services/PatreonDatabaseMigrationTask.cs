using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Patreon.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Patreon.Services
{
    public class PatreonDatabaseMigrationTask : IStartupTask
    {
        private readonly IServiceProvider _services;

        public PatreonDatabaseMigrationTask(IServiceProvider services)
        {
            _services = services;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            using IServiceScope serviceScope =
                _services.GetService<IServiceScopeFactory>()!.CreateScope();
            PatreonDatabaseContext dbContext =
                serviceScope.ServiceProvider.GetRequiredService<PatreonDatabaseContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
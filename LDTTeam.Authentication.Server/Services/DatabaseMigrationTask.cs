using System;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Server.Services
{
    public class DatabaseMigrationTask : IStartupTask
    {
        private readonly IServiceProvider _services;

        public DatabaseMigrationTask(IServiceProvider services)
        {
            _services = services;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            using IServiceScope serviceScope =
                _services.GetService<IServiceScopeFactory>()!.CreateScope();
            DatabaseContext dbContext =
                serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
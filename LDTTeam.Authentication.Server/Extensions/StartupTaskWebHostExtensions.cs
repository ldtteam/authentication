using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LDTTeam.Authentication.Modules.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Server.Extensions
{
    public static class StartupTaskWebHostExtensions
    {
        public static async Task RunWithTasksAsync(this IHost host, CancellationToken cancellationToken = default)
        {
            // Load all tasks from DI
            IEnumerable<IStartupTask> startupTasks = host.Services.GetServices<IStartupTask>();

            // Execute all the tasks
            IEnumerable<Task> tasks = startupTasks.Select(startupTask => startupTask.ExecuteAsync(cancellationToken));
            await Task.WhenAll(tasks);

            // Start the tasks as normal
            await host.RunAsync(cancellationToken);
        }
    }
}
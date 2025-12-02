using System.Threading.Tasks;
using LDTTeam.Authentication.Server.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Server
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunWithTasksAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration(x =>
                {
                    x.AddEnvironmentVariables("LDTTEAM_AUTH_");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureLogging(logging =>
                    {
                        logging.AddJsonConsole();
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
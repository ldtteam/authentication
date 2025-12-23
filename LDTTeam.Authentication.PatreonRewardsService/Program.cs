using JasperFx;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Extensions;
using LDTTeam.Authentication.PatreonRewardsService;
using LDTTeam.Authentication.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging().AddConfiguration();

builder
    .AddWolverine(opts =>
    {
        opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
        opts.Discovery.IncludeAssembly(typeof(PatreonConfig).Assembly);
    })
    .AddPatreonDatabase()
    .AddPatreonConfiguration()
    .AddPatreonTokenManagement()
    .AddPatreonApiService()
    .AddPatreonMembershipService()
    .AddRepositories();

var app = builder.Build();

app.MapGet("/", () => "LDTTeam Authentication Patreon Reward Service is running.");

app.MigrateDatabase();

await app.RunJasperFxCommands(args);

namespace LDTTeam.Authentication.PatreonRewardsService
{
    record Marker;
}
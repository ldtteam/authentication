using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Extensions;
using LDTTeam.Authentication.PatreonSyncService.Services;
using LDTTeam.Authentication.Utils.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddLogging().AddConfiguration();

builder
    .AddWolverine(opts =>
    {
        opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
        opts.Discovery.IncludeAssembly(typeof(PatreonConfig).Assembly);
    })
    .AddDatabase()
    .AddPatreonConfiguration()
    .AddPatreonTokenManagement()
    .AddPatreonApiService()
    .AddPatreonMembershipService()
    .AddRepositories();

builder.Services.AddHostedService<HostedPatreonSyncer>();

var app = builder.Build();

app.Run();

record Marker;


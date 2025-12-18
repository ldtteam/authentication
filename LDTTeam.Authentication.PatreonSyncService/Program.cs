using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Extensions;
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

var app = builder.Build();

app.Run();

record Marker;


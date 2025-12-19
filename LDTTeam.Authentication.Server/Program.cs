using LDTTeam.Authentication.Server;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging().AddConfiguration();
builder.AddWolverine();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

await startup.Configure(app, app.Environment);

app.Run();
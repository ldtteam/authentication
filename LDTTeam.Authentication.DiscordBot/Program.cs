// See https://aka.ms/new-console-template for more information

using JasperFx;
using LDTTeam.Authentication.DiscordBot.Extensions;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("LDTTEAM_AUTH_");

builder.AddDatabase()
    .AddWolverine(opts =>
    {
        opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
    })
    .AddRepositories()
    .AddDiscordOptions()
    .AddDiscord()
    .AddDiscordEventLogging()
    .AddDiscordRoleRewardManagement()
    .AddServer();

var app = builder.Build();

app.MapGet("/", () => "LDTTeam Authentication Discord Bot is running.");

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogWarning("Starting Rewards Service...");

app.MigrateDatabase();
var result = await app.RunJasperFxCommands(args);

logger.LogWarning("Application has stopped.");

logger.LogWarning("Registering Discord Slash Commands...");
using var scope = app.Services.CreateScope();
var serverProvider = scope.ServiceProvider.GetRequiredService<IServerProvider>();
var server = await serverProvider.GetServerAsync();

var slashService = scope.ServiceProvider.GetRequiredService<SlashService>();
await slashService.UpdateSlashCommandsAsync();
await slashService.UpdateSlashCommandsAsync(server.Id);
logger.LogWarning("Discord Slash Commands registered.");

return result;

record Marker();
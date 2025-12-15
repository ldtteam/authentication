// See https://aka.ms/new-console-template for more information

using System.Drawing;
using JasperFx;
using LDTTeam.Authentication.DiscordBot.Extensions;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
    builder.Logging.AddJsonConsole();

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables("LDTTEAM_AUTH_");
builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

builder.AddDatabase()
    .AddWolverine(opts => { opts.Discovery.IncludeAssembly(typeof(Marker).Assembly); })
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

logger.LogWarning("Registering Discord Slash Commands...");
using var scope = app.Services.CreateScope();
var serverProvider = scope.ServiceProvider.GetRequiredService<IServerProvider>();
var server = await serverProvider.GetServerAsync();

var slashService = scope.ServiceProvider.GetRequiredService<SlashService>();
await slashService.UpdateSlashCommandsAsync();
await slashService.UpdateSlashCommandsAsync(server.Id);

var eventLogger = scope.ServiceProvider.GetRequiredService<DiscordEventLoggingService>();
await eventLogger.LogEvent(new Embed()
{
    Title = "Bot Started",
    Description = "The Discord Bot has started and slash commands have been registered.",
    Colour = Color.DarkTurquoise,
    Fields = new List<EmbedField>
    {
        new("Server", $"{server.Server} ({server.Id})", false),
        new("Timestamp", DateTime.UtcNow.ToString("u"), false),
        new("Version", Environment.GetEnvironmentVariable("VERSION") ?? "Unknown", false)
    }
});

var result = await app.RunJasperFxCommands(args);

logger.LogWarning("Application has stopped.");

return result;

record Marker();
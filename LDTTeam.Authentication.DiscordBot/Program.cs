// See https://aka.ms/new-console-template for more information

using System.Drawing;
using JasperFx;
using LDTTeam.Authentication.DiscordBot.Extensions;
using LDTTeam.Authentication.DiscordBot.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddLogging()
    .AddConfiguration()
    .AddDatabase()
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
var servers = await serverProvider.GetServersAsync();

var slashService = scope.ServiceProvider.GetRequiredService<SlashService>();
foreach (var (_, id) in servers)
{
    await slashService.UpdateSlashCommandsAsync(id);
}
logger.LogWarning("Registering Discord Slash Commands for {ServerCount} servers...", servers.Count);

logger.LogWarning("Emitting bot started event to Discord Event Logging Service...");
var eventLogger = scope.ServiceProvider.GetRequiredService<DiscordEventLoggingService>();
await eventLogger.LogEvent(new Embed()
{
    Title = "Bot Started",
    Description = "The Discord Bot has started and slash commands have been registered.",
    Colour = Color.DarkTurquoise,
    Fields = new List<EmbedField>
    {
        new("Timestamp", DateTime.UtcNow.ToString("u"), false),
        new("Version", Environment.GetEnvironmentVariable("VERSION") ?? "Unknown", false)
    }.Union(servers.Select(server => new EmbedField(
        "Connected to: ",
        $"{server.Key} ({server.Value})",
        false
    ))).ToList()
});
logger.LogWarning("Emitted bot started event to Discord Event Logging Service.");

logger.LogInformation("Starting application with arguments: {Arguments}", args);
var result = await app.RunJasperFxCommands(args);

logger.LogWarning("Application has stopped.");

return result;

internal record Marker;
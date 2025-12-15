// See https://aka.ms/new-console-template for more information

using JasperFx;
using LDTTeam.Authentication.RewardsService.Extensions;
using LDTTeam.Authentication.RewardsService.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("LDTTEAM_AUTH_");

builder.AddDatabase()
    .AddWolverine()
    .AddRepositories()
    .AddCalculationService();

var app = builder.Build();

app.MapGet("/", () => "LDTTeam Authentication Rewards Service is running.");

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogWarning("Starting Rewards Service...");

app.MigrateDatabase();
var result = await app.RunJasperFxCommands(args);

logger.LogWarning("Application has stopped.");

return result;
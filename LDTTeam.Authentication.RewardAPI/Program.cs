using LDTTeam.Authentication.Models.App.User;
using LDTTeam.Authentication.RewardAPI;
using LDTTeam.Authentication.RewardAPI.Extensions;
using LDTTeam.Authentication.RewardAPI.Service;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging().AddConfiguration();

builder
    .AddWolverine(opts =>
    {
        opts.Discovery.IncludeAssembly(typeof(Marker).Assembly);
    })
    .AddDatabase()
    .AddRepositories();

// Add services to the container.

var app = builder.Build();

app.MapGet("/api/{accountProvider}/{providerKey}/{reward}", async (HttpContext context, string providerKey, AccountProvider accountProvider, string reward, [FromServices] IProviderLoginRepository loginRepository, ILogger<Marker> logger) =>
{
    logger.LogDebug("Received request for {AccountProvider} user {MinecraftUser} and reward {Reward}", accountProvider, providerKey, reward);
    var login = await loginRepository.GetByProviderAndProviderUserIdAsync(accountProvider, providerKey);
    if (login == null)
    {
        logger.LogDebug("No login found for {AccountProvider} user {MinecraftUser}", accountProvider, providerKey);
        return Results.NotFound();
    }
    
    logger.LogDebug("Found login for {AccountProvider} user {MinecraftUser}, checking reward {Reward}", accountProvider, providerKey, reward);
    var hasReward = login.User.Rewards.Any(r => r.Reward == reward);
    return Results.Ok(hasReward);
});


app.MigrateDatabase();

app.Run();

namespace LDTTeam.Authentication.RewardAPI
{
    record Marker;
}
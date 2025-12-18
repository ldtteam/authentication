using System.Text.Json;
using JasperFx.Core;
using LDTTeam.Authentication.PatreonApiUtils.Config;
using LDTTeam.Authentication.PatreonApiUtils.Extensions;
using LDTTeam.Authentication.PatreonApiUtils.Model.Requests;
using LDTTeam.Authentication.PatreonApiUtils.Service;
using LDTTeam.Authentication.PatreonWebhookService.Extensions;
using LDTTeam.Authentication.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

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
    .AddRepositories()
    .AddHMACValidation();

var app = builder.Build();

app.UseWebhook();
app.MapGet("/", () => "LDTTeam Authentication Patreon Webhook Service is running.");

app.MapPost("/webhook", async (HttpContext context, [FromServices] IPatreonMembershipService membershipService, [FromServices] ILogger<Marker> logger) =>
{
    var webhookBody = await context.Request.Body.ReadAllTextAsync();
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    var response = JsonSerializer.Deserialize<CampaignMemberResponse?>(webhookBody, options);

    if (response?.Data.Id == null)
    {
        logger.LogCritical("Received invalid Patreon webhook payload: {Payload}", webhookBody);
        context.Response.StatusCode = 400;
        return;
    }
    
    logger.LogInformation("Received Patreon membership webhook for Member ID {MemberId}", response?.Data.Id);
    await membershipService.UpdateStatusForMember(Guid.Parse(response?.Data.Id!));
});

app.MigrateDatabase();

app.Run();

record Marker;


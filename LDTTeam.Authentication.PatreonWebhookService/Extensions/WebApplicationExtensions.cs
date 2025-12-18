using LDTTeam.Authentication.PatreonWebhookService.Middleware;

namespace LDTTeam.Authentication.PatreonWebhookService.Extensions;

// ReSharper disable once InconsistentNaming
public static class WebApplicationExtensions
{
    extension(WebApplication host)
    {
        public void UseWebhook()
        {
            host.UseMiddleware<WebhookAuthenticationMiddleware>();
        }
    }
}
using LDTTeam.Authentication.PatreonWebhookService.Config;
using LDTTeam.Authentication.PatreonWebhookService.Service;

namespace LDTTeam.Authentication.PatreonWebhookService.Extensions;

// ReSharper disable once InconsistentNaming
public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddHMACValidation()
        {
            builder.Services.AddOptions<WebhookAuthenticationConfig>()
                .BindConfiguration("Webhook");
            builder.Services.AddSingleton<IHmacAuthenticationService, HmacAuthenticationService>();
            return builder;
        }
    }
}
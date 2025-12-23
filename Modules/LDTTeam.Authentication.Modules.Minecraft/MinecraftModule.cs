using System;
using AspNet.Security.OAuth.Minecraft;
using LDTTeam.Authentication.Modules.Api;
using LDTTeam.Authentication.Modules.Minecraft.Config;
using LDTTeam.Authentication.Modules.Minecraft.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Minecraft
{
    public class MinecraftModule : IModule
    {
        public string ModuleName => "Minecraft";

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration,
            AuthenticationBuilder builder)
        {
            MinecraftConfig? minecraftConfig = configuration.GetSection("minecraft").Get<MinecraftConfig>();

            if (minecraftConfig == null)
                throw new Exception("minecraft not set in configuration!");
            
            return builder.AddMinecraft(o =>
            {
                o.ClientId = minecraftConfig.ClientId;
                o.ClientSecret = minecraftConfig.ClientSecret;

                o.Scope.Add("Xboxlive.signin");
                o.Scope.Add("Xboxlive.offline_access");
                o.SaveTokens = true;
            });
        }

        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services,
            WebApplicationBuilder builder)
        {
            return services.AddTransient<MinecraftService>();
        }
    }
}
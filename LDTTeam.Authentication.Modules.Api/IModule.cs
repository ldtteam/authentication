using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LDTTeam.Authentication.Modules.Api
{
    public interface IModule
    {
        public string ModuleName { get; }

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration, AuthenticationBuilder builder)
        {
            return builder;
        }
        
        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services,
            IHostApplicationBuilder builder)
        {
            return services;
        }

        public Task OnUserSignIn(ClaimsPrincipal infoPrincipal, ApplicationUser user, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}
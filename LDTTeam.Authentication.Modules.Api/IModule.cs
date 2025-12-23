using System;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Api
{
    public interface IModule
    {
        public string ModuleName { get; }

        public AuthenticationBuilder ConfigureAuthentication(IConfiguration configuration, AuthenticationBuilder builder)
        {
            return builder;
        }
        
        public IServiceCollection ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            return services;
        }
    }
}
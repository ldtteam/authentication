using System;
using LDTTeam.Authentication.Modules.Api.Events;
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

        public void EventsSubscription(IServiceProvider services, EventsService events)
        {
        }
    }
}
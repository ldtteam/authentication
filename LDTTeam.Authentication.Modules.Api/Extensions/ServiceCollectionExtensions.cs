using Microsoft.Extensions.DependencyInjection;

namespace LDTTeam.Authentication.Modules.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
            where T : class, IStartupTask
            => services.AddTransient<IStartupTask, T>();
    }
}
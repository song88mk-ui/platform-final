using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Utility;

namespace Platform.Sportsbook.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProviders(this IServiceCollection services)
        {
            // Providers

            // Factory

            // HTTP clients

            //Other Services
            services.AddSingleton<IMemCache, MemCache>();

            return services;
        }
    }
}

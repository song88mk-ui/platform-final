using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Providers;
using Platform.Shared.Repositories;
using Platform.Shared.Utility;
using Platform.Shared.Providers.BGLiveStream;

namespace Platform.Sportsbook.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProviders(this IServiceCollection services)
        {
            // Providers
            services.AddScoped<ILiveStreamProvider, BetGeniusLiveStream>();

            // Factory
            services.AddScoped<ILiveStreamFactory, LiveStreamFactory>();

            // HTTP clients
            services.AddHttpClient<IBetGeniusRepository, BetGeniusRepository>();

            //Other Services
            services.AddSingleton<IMemCache, MemCache>();

            return services;
        }
    }
}

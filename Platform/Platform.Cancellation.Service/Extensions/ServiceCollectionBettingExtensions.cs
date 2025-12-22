using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Providers.GTS;
using Platform.Shared.Providers.MTS;
using Platform.Shared.Providers;
using Platform.Shared.Repositories;
using Platform.Shared.Services;

namespace Platform.Cancellation.Service
{
    public static class ServiceCollectionBettingExtensions
    {
        public static IServiceCollection AddBettingProviders(this IServiceCollection services)
        {
            // Providers
            services.AddScoped<IBetProvider, MtsBetProvider>();
            services.AddScoped<IBetProvider, GTSBetProvider>();

            // Factory
            services.AddScoped<IBetProviderFactory, BetProviderFactory>();

            // GTS HTTP clients
            services.AddHttpClient<IGTSAuthRepository, GTSAuthRepository>();
            services.AddHttpClient<IGTSRepository, GTSRepository>();

            // MTS HTTP clients
            services.AddHttpClient<IMTSAuthRepository, MTSAuthRepository>();
            services.AddHttpClient<IMTSMaxStakeRepository, MTSMaxStakeRepository>();

            //Other Services
            services.AddScoped<IParlayBonusService, ParlayBonusService>();

            return services;
        }
    }
}

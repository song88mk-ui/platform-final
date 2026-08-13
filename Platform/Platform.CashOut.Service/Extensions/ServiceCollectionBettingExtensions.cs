using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Providers.GTS;
using System;
using Platform.Shared.Providers.MTS;
using Platform.Shared.Providers;
using Platform.Shared.Repositories;
using Platform.Shared.Services;
using Platform.Shared.Utility;

namespace Platform.CashOut.Service
{
    public static class ServiceCollectionBettingExtensions
    {
        public static IServiceCollection AddBettingProviders(this IServiceCollection services)
        {
            // Factory
            services.AddScoped<IBetProviderFactory, BetProviderFactory>();

            // GTS HTTP clients. The cashout eligibility gate runs inside CashOut.Service's open
            // TransactionScope (default timeout 60s), so cap the HTTP timeout well below it —
            // a slow/down GTS then degrades to a fast fail-fast decline instead of pinning
            // worker threads for the 100s HttpClient default.
            services.AddHttpClient<IGTSAuthRepository, GTSAuthRepository>(c => c.Timeout = TimeSpan.FromSeconds(10));
            services.AddHttpClient<IGTSRepository, GTSRepository>(c => c.Timeout = TimeSpan.FromSeconds(10));

            // MTS HTTP clients
            services.AddHttpClient<IMTSAuthRepository, MTSAuthRepository>();
            services.AddHttpClient<IMTSMaxStakeRepository, MTSMaxStakeRepository>();

            // Providers
            services.AddScoped<IBetProvider, MtsBetProvider>();
            services.AddScoped<IBetProvider, GTSBetProvider>();

            //Other Services
            services.AddScoped<IParlayBonusService, ParlayBonusService>();
            services.AddSingleton<IMemCache, MemCache>();

            return services;
        }
    }
}

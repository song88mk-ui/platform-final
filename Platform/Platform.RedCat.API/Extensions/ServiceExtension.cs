using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.RedCat.API.Repositories;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using System;

namespace Platform.RedCat.API.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddContexts(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextPool<PlatformContext>(o => o.UseSqlServer(configuration.GetConnectionString("Platform")));

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IMemCache, MemCache>();
            services.AddScoped<IOperatorRepository, OperatorRepository>();
            services.AddScoped<IPlatformRepository, PlatformRepository>();

            return services;
        }
        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IBettingRepository, BettingRepository>(client =>
                client.BaseAddress = new Uri(configuration["API:RedCatBettingURI"])
            );

            return services;
        }

    }
}

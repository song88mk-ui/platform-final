using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.RedCat.API.Repositories;
using Platform.Shared.Data.Platform;

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
            services.AddScoped<IOperatorRepository, OperatorRepository>();
            services.AddScoped<IPlatformRepository, PlatformRepository>();

            return services;
        }

    }
}

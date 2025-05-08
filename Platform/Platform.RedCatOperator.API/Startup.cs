using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.RedCatOperator.API.Extensions;
using Platform.RedCatOperator.API.Repositories;
using Platform.Shared.Data.Platform;

namespace Platform.RedCatOperator.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTelemetry(Configuration);
            services.AddHealthChecks();
            services.AddDbContextPool<PlatformContext>(o => o.UseSqlServer(Configuration.GetConnectionString("Platform")));
            services.AddHttpClient<IRabbitMQRepository, RabbitMQRepository>();
            services.AddScoped<IOperatorRepository, OperatorRepository>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });
        }
    }
}

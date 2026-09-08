using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.RedCatBetting.API.Telemetry;
using Platform.RedCatBetting.API.Repositories;
using Platform.Shared.Data.RedCatBetting;
using System;
using Platform.Shared.Extensions;

namespace Platform.RedCatBetting.API
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
            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings(
                ServiceName: Instrumentation.ActivitySourceName,
                ActivitySourceName: Instrumentation.ActivitySourceName,
                MeterName: Instrumentation.MeterName
            ));
            services.AddHealthChecks();
            services.AddDbContextPool<RedCatBettingContext>(o => o.UseSqlServer(Configuration.GetConnectionString("RedCatBetting")));
            services.AddHttpClient<IOperatorRepository, OperatorRepository>(client =>
                client.BaseAddress = new Uri(Configuration["API:RedCatOperatorURI"])
            );
            services.AddScoped<IBettingRepository, BettingRepository>();
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

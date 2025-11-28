using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Platform.Oddin.API.Repository;
using Platform.Oddin.API.Telemetry;
using Platform.Shared.Data.ESportBetting;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using Platform.Shared.Telemetry;
using Platform.Shared.Extensions;

namespace Platform.Oddin.API
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
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                       .SetPreflightMaxAge(System.TimeSpan.FromHours(24))
                       .AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
                    });
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddControllers();

            services.AddHttpClient<IOperatorRepository, OperatorRepository>();
            services.AddHttpClient<IRabbitMQRepository, RabbitMQRepository>();
            services.AddScoped<IESportsBettingRepository, ESportsBettingRepository>();
            services.AddScoped<IMemCache, MemCache>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Platform Oddin API", Version = "v1" });
            });

            services.AddDbContext<PlatformContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Platform"]));
            services.AddDbContext<ESportBettingContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:ESportBetting"]));

            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings (
                ServiceName: Instrumentation.ActivitySourceName,
                ActivitySourceName: Instrumentation.ActivitySourceName,
                MeterName: Instrumentation.MeterName
            ));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform Oddin API V1");
            });

            app.UseRouting();

            app.UseCors("AllowAllOrigins");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

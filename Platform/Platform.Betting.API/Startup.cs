using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Platform.Betting.API.Repository;
using Platform.Betting.API.Telemetry;
using Platform.Shared.Data.Betting;
using Platform.Shared.Utility;
using Platform.Shared.Extensions;
using Platform.Shared.Repositories;
using Platform.Betting.API.Extensions;

namespace Platform.Betting.API
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
                        builder.AllowAnyOrigin()
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

            services.AddBettingProviders();

            services.AddScoped<IBettingRepository, BettingRepository>();
            services.AddScoped<IBetConfigRepository, BetConfigRepository>();

            services.AddHttpClient<IMTSProbabilityRepository, MTSProbabilityRepository>();
            services.AddHttpClient<ISportsbookRepository, SportsbookRepository>();
            services.AddHttpClient<IRabbitMQRepository, RabbitMQRepository>();


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Platform Betting API", Version = "v1" });
            });

            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings(
                ServiceName: Instrumentation.ActivitySourceName,
                ActivitySourceName: Instrumentation.ActivitySourceName,
                MeterName: Instrumentation.MeterName
            ));

            services.AddDbContext<BettingContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Betting"]));
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform Betting API V1");
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

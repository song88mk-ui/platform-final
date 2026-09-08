using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Platform.API.ActionFilters;
using Platform.API.Repository;
using Platform.API.Telemetry;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using Platform.Shared.Extensions;
using System.IO.Compression;
using System.Linq;

namespace Platform.API
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
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes =
                    ResponseCompressionDefaults.MimeTypes.Concat(
                        new[] { "image/svg+xml" });
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

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

            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings(
                ServiceName: Platform.API.Telemetry.Instrumentation.ACTIVITY_SOURCE_NAME,
                ActivitySourceName: Platform.API.Telemetry.Instrumentation.ACTIVITY_SOURCE_NAME,
                MeterName: Platform.API.Telemetry.Instrumentation.METER_NAME
            ));

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddScoped<APIKeyFilter>();
            services.AddScoped<InternalAPIKeyFilter>();

            services.AddControllers();

            services.AddSingleton<IMemCache, MemCache>();
            // Publish-only broker — used by DeleteCachePreload to forward OnTranslationChange to
            // Platform.CacheSync.Service. Nothing calls Connect(): publishes MUST use the 5-arg
            // CPublish overload (lazy GetConnection + own channel); the 4-arg overloads require a
            // prior Connect() and would NullReference. MessageBroker:Queue is intentionally empty
            // so the lazy connect never declares a queue or starts consuming.
            services.AddSingleton<IMessageBroker, MessageBroker>();
            services.AddScoped<IPlatformRepository, PlatformRepository>();
            services.AddHttpClient<IBettingRepository, BettingRepository>();
            services.AddHttpClient<IOperatorRepository, OperatorRepository>();
            services.AddHttpClient<ISportsbookRepository, SportsbookRepository>();
            services.AddHttpClient<ICasinoRepository, CasinoRepository>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Platform API", Version = "v1" });
            });

            services.AddDbContext<PlatformContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Platform"]));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseResponseCompression();

            app.UseForwardedHeaders();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform API V1");
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

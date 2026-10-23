using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Platform.Operator.API.ActionFilters;
using Platform.Operator.API.Repository;
using Platform.Operator.API.Telemetry;
using Platform.Shared.Data;
using Platform.Shared.Data.Platform;

namespace Platform.Operator.API
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
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddScoped<IPlatformRepository, PlatformRepository>();
            services.AddScoped<IOperatorRepository, OperatorRepository>();
            services.AddHttpClient<IAuthRepository, AuthRepository>();
            services.AddHttpClient<IWalletRepository, WalletRepository>();
            services.AddHttpClient<IReportsRepository, ReportRepository>();

            services.AddScoped<InternalAPIKeyFilter>();

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Operator API", Version = "v1" });
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

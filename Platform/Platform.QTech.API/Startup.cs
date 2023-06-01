using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Platform.QTech.API.ActionFilters;
using Platform.QTech.API.Repository;
using Platform.Shared.Data.CasinoBetting;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;

namespace Platform.QTech.API
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

            services.AddScoped<PassKeyFilter>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddControllers();

            services.AddSingleton<IMemCache, MemCache>();
            services.AddScoped<ICasinoRepository, CasinoRepository>();
            services.AddHttpClient<IProviderRepository, ProviderRepository>();
            services.AddHttpClient<IOperatorRepository, OperatorRepository>();
            services.AddHttpClient<IRabbitMQRepository, RabbitMQRepository>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Platform QTech API", Version = "v1" });
            });

            services.AddDbContext<PlatformContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Platform"]));
            services.AddDbContext<CasinoBettingContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:CasinoBetting"]));
            services.AddApplicationInsightsTelemetry();
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform QTech API V1");
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

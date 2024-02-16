using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.ClientSportsbook.API.ActionFilters;
using Platform.ClientSportsbook.API.Repository;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;
using System;

namespace Platform.ClientSportsbook.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Client Sportsbook API", Version = "v1" });
            //    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            //    {
            //        Name = "x-api-key",
            //        In = ParameterLocation.Header,
            //        Type = SecuritySchemeType.ApiKey,
            //        Description = "Authorization by x-api-key inside request's header",
            //        Scheme = "ApiKeyScheme"
            //    });
            //    var key = new OpenApiSecurityScheme()
            //    {
            //        Reference = new OpenApiReference
            //        {
            //            Type = ReferenceType.SecurityScheme,
            //            Id = "ApiKey"
            //        },
            //        In = ParameterLocation.Header
            //    };

            //    var requirement = new OpenApiSecurityRequirement { { key, new List<string>() } };
            //    c.AddSecurityRequirement(requirement);
            //});

            services.AddSingleton<IMemCache, MemCache>();
            services.AddScoped<AuthorizationFilter>();
            services.AddSingleton<RedisCache>();
            services.AddHttpClient<ISportsbookRepository, SportsbookRepository>(client =>
            {
                client.BaseAddress = new Uri(Configuration["API:SportsbookAPI"]);
            });
            services.AddHttpClient<IRabbitMQRepository, RabbitMQRepository>(client =>
            {
                client.BaseAddress = new Uri(Configuration["API:RabbitMQAPI"]);
            });
            services.AddDbContext<PlatformContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Platform"]));
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

            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Client Sportsbook API V1");
            //});
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
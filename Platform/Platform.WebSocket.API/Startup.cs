using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Data;
using Platform.WebSocket.API.Repository;
using System;
using System.Linq;

namespace Platform.WebSocket.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddControllers();

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            if (Convert.ToBoolean(Configuration["Custom:IsProduction"]))
            {
                services.AddSignalR(hubOptions =>
                {
                    hubOptions.MaximumReceiveMessageSize = 5000000;
                })
                .AddAzureSignalR("Endpoint=https://ws.song88.com;AccessKey=5ES+/AH0OiMLbDQrsMZnXmNFpkHuUlEH2dfn3sFnRyw=;Version=1.0;");
            }
            else
            {
                services.AddSignalR(hubOptions =>
                {
                    hubOptions.MaximumReceiveMessageSize = 5000000;
                });
            }

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                           .SetIsOriginAllowed(origin => true)
                           .AllowAnyHeader()
                           .AllowAnyMethod()                           
                           .AllowCredentials();
                    });
            });

            services.AddDbContext<PlatformContext>(o => o.UseSqlServer(Configuration["ConnectionStrings:Platform"]));
            services.AddSingleton<IMemCache, MemCache>();
            services.AddSingleton<IWebSocketRepository, WebSocketRepository>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebSocket API", Version = "v1" });
            });

            services.AddApplicationInsightsTelemetry();
        }

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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebSocket API V1");
            });

            app.UseRouting();

            app.UseCors("AllowAllOrigins");

            app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<WebSocketRepository>("/platform", options =>
                {
                    options.ApplicationMaxBufferSize = 5000000;
                    options.TransportMaxBufferSize = 5000000;
                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                });
                endpoints.MapControllers();
            });
        }
    }
}

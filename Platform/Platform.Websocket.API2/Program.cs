using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Platform.Shared.Utility;
using Platform.WebSocket.API2.Data;
using Platform.WebSocket.API2.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebSocket MessagePack API", Version = "v1" });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

if (bool.Parse(builder.Configuration["Custom:UseMessagePack"]))
{
    builder.Services.AddSignalR(hubOptions =>
    {
        hubOptions.MaximumReceiveMessageSize = 5000000;
    })
    .AddMessagePackProtocol(options =>
    {
        StaticCompositeResolver.Instance.Register(
            ContractlessStandardResolver.Instance
        );
        options.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);
    });
}
else
{
    builder.Services.AddSignalR(hubOptions =>
    {
        hubOptions.MaximumReceiveMessageSize = 5000000;
    });
}

builder.Services.AddCors(options =>
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

builder.Services.AddDbContext<PlatformContext>(o => o.UseSqlServer(builder.Configuration["ConnectionStrings:Platform"]));
builder.Services.AddSingleton<IMemCache, MemCache>();

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebSocket MessagePack API V1");
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

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

app.Run();

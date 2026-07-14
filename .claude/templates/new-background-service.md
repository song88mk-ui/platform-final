# Template: New Background Service (Worker)

Mirror an existing `*.Service` (e.g. `Settlement.Service`).

## 1. Project: `Microsoft.NET.Sdk.Worker`, `netcoreapp3.1`, reference `Platform.Shared`.

## 2. `Program.cs`
```csharp
public static void Main(string[] args) {
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true); // OTLP
    CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx,_,log) => log.ReadFrom.Configuration(ctx.Configuration))
        // [.UseWindowsService()]  // only if needed
        .ConfigureServices((ctx, services) => {
            services.AddSingleton<Instrumentation>();
            services.AddTelemetry(new TelemetrySettings(Instrumentation.ActivitySourceName,
                Instrumentation.ActivitySourceName, Instrumentation.MeterName));
            services.AddSingleton<IMessageBroker, MessageBroker>();
            services.AddSingleton<IXxxRepository, XxxRepository>();
            // services.AddDbContext<...>(o => o.UseSqlServer(ctx.Configuration["ConnectionStrings:X"]));
            services.AddHostedService<Worker>();
        });
```

## 3. `Worker : BackgroundService`
- In `ExecuteAsync`: connect broker (`Connect`/`ConnectWithManualAck`), subscribe `MessageReceived`,
  `switch(MessageType)`. Preload state if needed. Loop-jobs use `while(!ct) { work; await Task.Delay }`.
- **DbContext lifetime:** create per message (don't cache a `DbContext` in a singleton repo — R-13).

## 4. Add a `Dockerfile`, an appsettings (no secrets), the CI matrix entry, and an argocd app.
## 5. `/update-docs` (ProjectOverview, DependencyGraph).

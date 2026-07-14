# /find-pattern

Find the established way the team does something (so you match it, not reinvent).

## Steps
1. Check [docs/ImplementationPatterns.md](../../docs/ImplementationPatterns.md) (present/absent map) +
   [docs/BestPractices.md](../../docs/BestPractices.md) (house rules).
2. Find a real example in code (grep a similar repository/controller/worker) and mirror its shape.
3. Confirm it's not an [docs/AntiPatterns.md](../../docs/AntiPatterns.md) item before copying.

## Quick answers
- DI grouping → `IServiceCollection` extension method · Provider → `IBetProvider`+factory ·
  Cache → `IMemCache.GetCache(key,loader,hours)` · Mapping → manual `.Select` · Validation →
  ActionFilter + `StatusCode` · Messaging → `On*` + `MessageReceived` switch · Config → `IConfiguration["A:B"]`.
- **Absent** (don't introduce): AutoMapper, FluentValidation, MediatR/CQRS, GoF Builder, `IOptions<T>`
  app config, EF migrations.

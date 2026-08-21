# Skill: Feature Development

Add a backend feature the way this team builds them.

## Workflow
1. **Trace the nearest existing feature** (`/trace-feature`) and copy its shape.
2. **Match patterns** ([docs/ImplementationPatterns.md](../../docs/ImplementationPatterns.md),
   [docs/BestPractices.md](../../docs/BestPractices.md)): repository (`IXxxRepository`+impl) returning
   `Task<BaseResponseDTO<T>>`; thin controller with a `[ServiceFilter]`; DI via extension method;
   config via `IConfiguration["A:B"]`; manual `.Select` mapping.
3. **Pick the data path:** EF LINQ (`AsNoTracking`/parameterized proc), Mongo read-only projection,
   or HttpClient — see [docs/RepositoryPatterns.md](../../docs/RepositoryPatterns.md).
4. **Async?** define an `On*` contract in `Platform.Shared/MessageModels`, publish via `IMessageBroker`
   (or `RabbitMQ.API`), consume in a `Worker` with the right ack mode + `CorrelationId`.
5. **Cross-cutting:** OTel `Activity` + Serilog; read-through `IMemCache` + invalidation if cached.
6. **Impact:** run `/impact-analysis` (esp. if touching `Platform.Shared` or a contract).
7. **Finish:** `/review-feature` + [.claude/checklists/before-pull-request.md](../checklists/before-pull-request.md);
   `/update-docs`.

## Guardrails
3.1-compatible; new controller ⇒ filter; parameterized SQL; no new secrets; no new pattern when an
established one exists (don't add AutoMapper/FluentValidation/MediatR/`IOptions<T>`). UI ⇒ platform-web.

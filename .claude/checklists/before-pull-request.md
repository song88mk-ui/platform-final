# Checklist: Before Pull Request

## Conformance
- [ ] Repository + `BaseResponseDTO<T>`/`StatusCode`; thin controller; manual `.Select` mapping.
- [ ] DI via extension method where grouping; `IConfiguration` indexer (no `IOptions<T>`).
- [ ] New controller has the correct `[ServiceFilter]` (API vs internal key).

## Safety
- [ ] No committed secrets; no secret values in logs/responses.
- [ ] SQL parameterized (no interpolated `FromSqlRaw`); money via Wallet proc.
- [ ] No `ex.Message`/`StackTrace` returned to callers; guarded header/input parsing.
- [ ] No Mongo writes from platform-final.

## Cross-cutting
- [ ] OTel `Activity` + Serilog; trace context propagated through RabbitMQ (if messaging).
- [ ] Cache read-through + invalidation wired (if config/curation cached).
- [ ] Correct ack mode + `CorrelationId` (if consuming messages).

## Impact & docs
- [ ] `/impact-analysis` done; cross-repo contracts verified against odds-engine-2/platform-web (R-2).
- [ ] Builds (.NET Core 3.1-compatible). Tests updated if applicable (only `Shared.Test` exists).
- [ ] `/update-docs` — affected `docs/` updated with confidence tags + evidence.
- [ ] Commit/PR only if the user asked; hooks not bypassed.

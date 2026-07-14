# Utilities & Extension Methods (Quick Reference)

> Phase 9. The shared utility classes, helpers, and extension methods in `Platform.Shared`.
> Authoritative: [SharedLibraries.md](../SharedLibraries.md). All **[Verified]** (files) / **[Likely]**
> (exact method signatures, inferred from names where not line-read).

## `Platform.Shared/Utility/` `[Verified]`
| File | Purpose |
|------|---------|
| `MessageBroker.cs` / `IMessageBroker.cs` | RabbitMQ wrap: `Connect`/`ConnectWithManualAck`/`ConnectWithAutoAck`/`ExternalConnect`; `Publish<T>`/`CPublish<T>`/`ExternalPublish<T>`; `Acknowledge`/`NAcknowledge`; `event EventHandler<BasicDeliverEventArgs> MessageReceived`. Topic exchange, persistent, heartbeat 60s, recovery 5s, TLS @5671, W3C trace headers. |
| `MemCache.cs` / `IMemCache.cs` | In-proc cache over static `MemoryCache.Default`: `AddOrUpdateCache<T>(key,obj,hours|DateTimeOffset)`, `GetCache<T>(key)`, `GetCache<T>(key,loader,hours)` (lazy), `GetCache(key)→KeyValuePair<bool,long>`, `DeleteCache(key)`, `DeleteCacheByPrefix(prefix)`. |
| `Helper.cs` / `IHelper.cs` | general-purpose helper (used by `CacheSync.Service` etc.). |
| `JwtParser.cs` | JWT decode utility — **defined but unused in platform-final** ([Authentication.md](../Authentication.md)). |
| `Formatting.cs` | string/number formatting. |

## `Platform.Shared/Helpers/` `[Verified]`
| File | Purpose |
|------|---------|
| `OddsTypeHelper.cs` | `GetOddsByOddsType(oddsTypeId, decimalOdds, isBinary)` → converts decimal odds to Malay/HK/Indo (binary-market rules); the single source of odds-format conversion. |
| `DecimalJsonConverter.cs` | custom `System.Text.Json` converter for decimal precision. |

## `Platform.Shared/Services/` `[Verified]`
| File | Purpose |
|------|---------|
| `ParlayBonusService .cs` / `IParlayBonusService.cs` | `GetParlayBonus(clientId, currencyCode, betDate, selections, payout)` → `(bool HasBonus, ParlayBonusAudit Config)`; opens its own `BettingContext`, `TransactionScope` ReadCommitted. (Filename has a trailing space.) |

## `Platform.Shared/Extensions/` (10 files) `[Verified]` files / `[Likely]` methods
| File | Extends | Typical use |
|------|---------|-------------|
| `DateTimeExtensions.cs` | `DateTime` | UTC/offset math (used heavily for fixture dates) |
| `DecimalExtensions.cs` | `decimal` | odds/stake/payout rounding |
| `StringExtensions.cs` | `string` | parsing/formatting/guards |
| `HashExtensions.cs` | — | hashing (e.g. MD5/exclusive-queue/signature) |
| `IntExtensions.cs` | `int` | numeric helpers |
| `ListExtensions.cs` | `IEnumerable`/`List` | batching/null-safe ops |
| `ObjectExtensions.cs` | `object` | serialization/reflection helpers |
| `EnumExtensions.cs` | `enum` | parse/validate/describe |
| `BetAuditEntityExtension.cs` | `Bet` | build a `BetAudit` snapshot |
| `BetSelectionAuditEntityExtension.cs` | `BetSelection` | build a `BetSelectionAudit` snapshot |

> The two `*AuditEntityExtension` methods are how the **append-only audit** pattern is applied
> ([PersistencePatterns.md](../PersistencePatterns.md)).

## DI-grouping extension methods (not in `Extensions/`) `[Verified]`
`AddBettingProviders` (Betting.API/Extensions), `AddProviders` (Sportsbook.API/Extensions), RedCat
`ServiceExtension.{AddContexts,AddHttpClients,AddRepositories}`, `AddTelemetry` (`Infra.Extensions.Telemetry`).

## Per-project utilities `[Verified]`
- `Telemetry/Instrumentation.cs` (every instrumented project) — `ActivitySource`+`Meter`.
- `ActionFilters/` — `APIKeyFilter`, `InternalAPIKeyFilter`, `AuthorizationFilter`, `PassKeyFilter`.
- Some services carry a **duplicated** `Utility/MessageBroker.cs` (e.g. `BetStatus.Service`) — prefer
  the `Platform.Shared` one ([AntiPatterns.md](../AntiPatterns.md) AP-8).

## Quick "which util do I need?"
| Task | Util |
|------|------|
| convert odds to a display format | `OddsTypeHelper.GetOddsByOddsType` |
| cache a lookup with TTL | `IMemCache.GetCache(key, loader, hours)` |
| publish/consume a message | `IMessageBroker` |
| snapshot a bet for audit | `BetAuditEntityExtension` / `BetSelectionAuditEntityExtension` |
| serialize a decimal precisely | `DecimalJsonConverter` |
| evaluate parlay bonus | `IParlayBonusService.GetParlayBonus` |

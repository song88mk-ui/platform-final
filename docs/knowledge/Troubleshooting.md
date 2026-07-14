# Troubleshooting Guide

> Phase 9. Symptom → likely cause → where to look, synthesized from the platform's failure modes,
> `StatusCode` semantics, and known anti-patterns. **[Verified]** mechanisms / **[Likely]** diagnoses.
> Cross-refs: [PublicApiSurface.md](../PublicApiSurface.md), [BusinessRules.md](../BusinessRules.md),
> [AntiPatterns.md](../AntiPatterns.md), [CacheInvalidation.md](../CacheInvalidation.md).

## Reading errors
- **Always check the body `StatusCode`, not the HTTP code** — business errors return **HTTP 400**
  with `BaseResponseDTO{StatusCode,Message}`; success/maintenance is 200. A bare **500** = an
  exception escaped a controller (no global handler — [MiddlewarePipeline.md](../MiddlewarePipeline.md)).

## API / auth symptoms
| Symptom | StatusCode | Likely cause / where |
|---------|-----------|----------------------|
| All calls rejected | 101 | wrong/missing `x-api-key` vs `Custom:APIKey`/`InternalAPIKey`; check SSM-injected `Custom__APIKey` |
| Rejected with "client" | 102 | `x-client-id` not a Guid / inactive `Client` row; `AuthorizationFilter` (24h `cc:` cache may be stale) |
| Rejected with "operator token" / Operator_Error | 103 / 1001 | operator token invalid — validated **downstream** (Operator/Auth), not in platform-final |
| Language error | 106 | `x-language-code` not a defined `LanguageCode` |
| Bare 500 on an OG client | — | malformed `x-og-*` header → `int.Parse` throws in `APIKeyFilter` (AP-7) |
| Internal endpoint 401-ish | 101 | needs `Custom:InternalAPIKey` (back-office key), not the public key |

## Bet placement symptoms `[Verified]`
| Symptom | StatusCode |
|---------|-----------|
| "odds changed, reconfirm" | 126 Odds_Or_Lines_Have_Changed (consumer alias Odds_Is_Stale) |
| Bet refused — market | 124 Suspended / 125 Not_Offered / 128 Odds_Inactive / 129 Betstop_Fixture / 130 Betstop_Global |
| Bet refused — limits | 104 Insufficient_Funds / 114–116 stake min/max/per-game / 117 max payout / 131 tier liability |
| Provider error | 132 Mts_Error |
| Parlay/boost/builder rejected | 145 (virtuals) / 147–149 (boost) / 154–157 (bet-builder) |
| Restricted | 118–121 (global/sport/segment/league) |

*Thresholds are data* (`BetConfig` JSON per client+currency) — check `v1/bet/config`, not code.

## "Stale data" symptoms `[Verified]`
| Symptom | Cause | Where |
|---------|-------|-------|
| Config/curation change not showing | per-pod `IMemCache` not evicted on one replica (missed `CacheSync` message) OR platform-web's 30s cache | [CacheInvalidation.md](../CacheInvalidation.md) — restart pod or wait for TTL |
| Top fixtures/leagues wrong | curation written by back-office (API or **direct SQL**); check `ClientTopFixture`/ordering tables + invalidation | [CrossRepositoryDependencies.md](../CrossRepositoryDependencies.md) |
| Fixture/odds missing or wrong | catalogue is **odds-engine-2-owned** Mongo read model — issue is likely upstream (OE2 feed), not platform-final | [OddsEngineIntegration.md](../OddsEngineIntegration.md) |
| Bet references a fixture that "doesn't exist" | no cross-store FK; bet (SQL) snapshot vs Mongo catalogue diverged | [Relationships.md](../Relationships.md) |

## Real-time (WebSocket) symptoms `[Verified-sibling]`
| Symptom | Likely cause |
|---------|--------------|
| No live odds updates | hub (`Websocket.API2`) up but **feeder** `OE.Websocket.Service` (odds-engine-2) down, or RabbitMQ `r.oe.sb.engine.*` not flowing |
| Odds update but UI doesn't repaint | platform-web `ShouldRender`/`ValidateParameters` gating (component thinks nothing changed) |
| Bet status/cashout/wallet not pushing | platform-final `r.bet.*`/`r.wallet.*` producers (BetStatus/CashOut services) or the feeder relay |
| Connection drops | SignalR reconnect (3×5s then reload); MessagePack/`UseMessagePack` mismatch |

## Settlement / cashout symptoms `[Verified]`
| Symptom | Where |
|---------|-------|
| Bet stuck Running / not settled | `Settlement.Service` consuming `OnBetSettlement`? failed → `Job.Resettlement` retry (Fibonacci, `RetryCount<8`, `OperatorSettlementRetry`) |
| Cashout unavailable / value changed | 141 / 142 — `CashOut.Service` + `CashOutStake.Servicev2` (live value from `OnOddsChange`) |
| Wallet balance wrong | `Wallet.API` stored procs (`CreditOrDebitUserWallet`/`TransferUserWallet`); `decimal(26,12)` |
| Duplicate/partial cashout audit | `PartialCashoutAudit` rows (append-only); `IsCashout`/`IsPartialCashout` on `Bet` |

## Infra / startup symptoms `[Verified]`
| Symptom | Likely cause |
|---------|--------------|
| Service won't start / DB errors | wrong `ConnectionStrings__*` from SSM; RDS/DocumentDB reachability |
| OTLP/telemetry export fails | `Http2UnencryptedSupport` switch / collector endpoint / `Telemetry:Enabled` |
| RabbitMQ not connecting | `MessageBroker__Host`=`rabbitmq-main-cluster.bet-genius-apps.svc`, TLS @5671; broker auto-recovers (5s) so transient is OK |
| Reporting query odd/slow/injection-ish | `AgentReports` `FromSqlRaw` string interpolation (AP-2) |
| Config value ignored | typo'd key returns null silently (no `IOptions<T>` validation — AP-11) |

## Where to look first (by domain)
| Domain | First file |
|--------|-----------|
| Sportsbook reads | `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (the most-changed file) |
| Bet placement/config | `Platform.Betting.API/Repository/BettingRepository.cs` |
| Gateway/auth | `Platform.API/Repository/PlatformRepository.cs`, `Controllers/IntegrationController.cs`, `ActionFilters/` |
| Settlement | `Platform.Settlement.Service/Repository/BettingRepository.cs` |
| Messaging | `Platform.Shared/Utility/MessageBroker.cs` |
| Caching | `Platform.Shared/Utility/MemCache.cs`, `CacheSync.Service` |

See [FrequentlyModifiedAreas.md](FrequentlyModifiedAreas.md) for the change hotspots.

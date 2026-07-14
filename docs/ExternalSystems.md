# External Systems

> Phase 5. Third-party / infrastructure systems platform-final depends on **outside the workspace
> repos**. Classification: **[Verified]** in platform-final source/config · **[Verified-sibling]** in
> argocd/infrastructure · **[Likely]**/**[Unknown]**. Secret **values are never printed** — key names only.

## Betting / odds providers
| System | What it is | platform-final usage | Config keys | Evidence |
|--------|-----------|----------------------|-------------|----------|
| **Sportradar MTS** | bet validation / max-stake / risk (Betradar) | `MtsBetProvider` (ChannelType SR), `MTSAuthRepository`/`MTSMaxStakeRepository`, `CashOut.Service` MTS SDK (`Sportradar.MTS.SDKCore` 2.4.1.2) | `API:MTSAuthAPI`, `Custom:MTSClientSecret|MTSUsername|MTSPassword`, `Custom:IsMTSEnabled` | [Verified] |
| **Genius Sports / BetGenius (GTS)** | bet assessment + feed + livestream | `GTSBetProvider` (ChannelType BG), `GTSRepository`/`GTSAuthRepository`, `BetGeniusRepository`, `BetGeniusLiveStream` | `API:GTSAPI`, `Custom:GTSBookmakerId`, `Custom:BetGenius:Uri|User|Password` | [Verified] |
| **Ably** (ably.io) | realtime channel for **GTS delayed assessment** responses | `Provider.Service` subscribes, republishes `OnBetPlacement` | `Custom:Ably:ApiKey|Channel|ChannelCount` | [Verified] |
| **Oddin** | eSports odds provider | `Oddin.API` integration | `Custom:Oddin:BaseUrl|BrandToken` | [Verified] |

> Betradar UOF itself is consumed by **odds-engine-2 / oddsfeed**, not platform-final directly — see
> [OddsEngineIntegration.md](OddsEngineIntegration.md). **[Verified-sibling]**

## Wallet / casino / operator partners
| System | What it is | platform-final usage | Evidence |
|--------|-----------|----------------------|----------|
| **QTech** | casino game aggregator | `QTech.API` (sessions, balance, transactions, lobby); `PassKeyFilter`; Mongo `MongoDB-Casino` | [Verified] |
| **RedCat** | retail/virtual-sports operator/POS | `RedCat.API`/`RedCatBetting.API`/`RedCatOperator.API` callbacks; `Custom:SharedKey` | [Verified] |
| **Oriental Gaming (OG)** | operator integration with HMAC-signed launches | `APIKeyFilter` OG path, `x-og-*` headers, `Custom:OrientalGaming:*` | [Verified] |

## Real-time / messaging infra
| System | Role | platform-final usage | Evidence |
|--------|------|----------------------|----------|
| **RabbitMQ (AWS MQ)** | the `e.central` bus | `MessageBroker` (`MessageBroker:*`), AMQP/TLS 5671, `rabbitmq-main-cluster.bet-genius-apps.svc`, prefetch 1000 | [Verified] + [Verified-sibling] |
| **Azure SignalR Service** | managed SignalR backplane | `WebSocket.API`/`Websocket.API2` production hub; `Custom:IsProduction`/`EnableAzureSignalRService` | [Verified] + [Verified-sibling] |

## Datastores (managed)
| System | Role | platform-final usage | Provisioned by |
|--------|------|----------------------|----------------|
| **AWS RDS — SQL Server** | the 8 relational DBs | `ConnectionStrings:*` (Platform/Betting/Sportsbook/Translation/Wallet/AgentReports/Casino/ESport/RedCat); host `…rds.amazonaws.com:1433` | RDS (not yet in Terraform — managed separately) [Verified-sibling] |
| **AWS DocumentDB — MongoDB** | `sportsbook` catalogue read model | `ConnectionStrings:MongoDB-Sportsbook` (+ `MongoDB-Casino`); CA bundle installed in Docker | `infrastructure/documentdb` [Verified-sibling] |
| **Redis** | client sportsbook cache | `ClientSportsbook.API` (`StackExchange.Redis`) | [Verified] (provider) / host [Unknown] |

## Cloud / platform infrastructure `[Verified-sibling]`
| System | Role |
|--------|------|
| **AWS EKS** | runs all containers (Kestrel :80 → K8s service :3000) |
| **AWS ECR** | image registry; per-env accounts dev `418541840646` / stg `172065883315` / prd `525375333516` |
| **AWS SSM Parameter Store + External Secrets Operator** | runtime secret/config injection as `Section__Key` env vars (ClusterSecretStore `parameterstore`) |
| **ArgoCD (+ Image Updater)** | GitOps deploy + auto image-tag sync |
| **Cloudflare** | DNS + edge; `CF-Connecting-IP`/`CF-IPCountry` headers consumed by filters; ALB + ACM TLS |
| **OpenTelemetry collector / Tempo / Prometheus / Loki** | traces/metrics/logs (OTLP gRPC h2c); `monitoring` namespace | 

## Private build dependencies `[Verified]`
| System | Role |
|--------|------|
| **GitHub NuGet feed** (`nuget.pkg.github.com/FlyingCockatoo`) | private packages; Docker build needs `NUGET_PASSWORD` |
| **`Infra.Extensions.Telemetry`** (0.0.1-1, private) | the OpenTelemetry `AddTelemetry` extension used across services |
| **`Sportradar.MTS.SDKCore`** (2.4.1.2) | MTS SDK |

## External-call inventory (outbound HTTP from platform-final) `[Verified]`
- MTS auth/maxstake (`API:MTSAuthAPI`), GTS/BetGenius (`API:GTSAPI`, `Custom:BetGenius:Uri`),
  Oddin (`Custom:Oddin:BaseUrl`), QTech (`API:QTechAPI`), Betradar score/cricket (Job.Operator.Score),
  Forex source (Job.ForexEUR — endpoint **[Unknown]**), Ably (provider service).
- Internal-but-cross-service: `RabbitMQ.API`, Auth (`Operator.API/IAuthRepository`), Wallet.API.

## Trust-boundary notes `[Verified]`
- Apps sit **behind Cloudflare + ALB ingress**; `ForwardedHeaders` trusts all upstreams
  (KnownProxies cleared) — fine behind the controlled ingress, risky if exposed directly.
- Partner inbound callbacks (QTech `Pass-Key`, RedCat `SharedKey`, OG HMAC, OddsFeed status) are
  authenticated by **partner-specific keys/signatures**, not the platform API key.
- MTS/GTS/Betradar/Ably/Oddin credentials are **external secrets** (SSM in prod; dev values in
  committed `appsettings.json` — [TechnicalDebt.md](TechnicalDebt.md) TD-01).

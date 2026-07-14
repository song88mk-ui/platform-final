# Request Lifecycle (End-to-End)

> **Phase 4 — End-to-End Data Flow.** Analysis-only; no code modified. These traces deliberately
> **cross repository boundaries**: browser → **platform-web** (Blazor BFF) → **platform-final**
> (`Platform.API` gateway + services + MongoDB) → **odds-engine-2** (odds feed) → back.
>
> **Provenance tags** (since this spans repos):
> - **[PW]** verified in the platform-web sibling (its source / its `docs/`).
> - **[PF]** verified in platform-final source (Phases 1–3).
> - **[OE]** verified/inferred for odds-engine-2 (sibling).
>
> Diagrams for these flows: [SequenceDiagrams.md](SequenceDiagrams.md). UI render/state detail:
> [RenderingPipeline.md](RenderingPipeline.md), [StateFlow.md](StateFlow.md),
> [ComponentDataFlow.md](ComponentDataFlow.md). API surface: [PublicApiSurface.md](PublicApiSurface.md).
> Real-time: [WebSocketContracts.md](WebSocketContracts.md).

## The end-to-end skeleton (every flow follows a slice of this)
```
Browser
  ↓  HTTP(S) / Blazor circuit (WebSocket)
platform-web  (Blazor Server BFF — Platform.Child.Web)
  ├─ middleware: StaticFiles → MaintenanceMiddleware → Routing → MapBlazorHub(/blazor) → MapFallbackToPage(/_Host)   [PW]
  ├─ _Host.cshtml (SSR pre-render) → App.razor → Router → Layout → Page → Components                                [PW]
  ├─ State containers (OperatorState/TranslationState/PageState/DeviceState, scoped per circuit)                     [PW]
  ├─ IPlatformRepository (typed HttpClient, x-api-key + x-operator-token + x-client-id…)                             [PW]
  └─ HubConnection (SignalR → websocket-api/platform)                                                                [PW]
  ↓  HTTP REST                              ↓ SignalR (MessagePack)
platform-final Platform.API gateway        platform-final Websocket.API2 hub                                         [PF]
  ├─ pipeline: ForwardedHeaders → Swagger → Routing → CORS → UseAuthorization(no-op) → MapControllers              [PF]
  ├─ APIKeyFilter / AuthorizationFilter (ActionFilter) validates x-api-key/x-client-id                              [PF]
  ├─ IntegrationController → typed HttpClient repos → Betting.API / Sportsbook.API / Wallet.API / QTech            [PF]
  └─ Sportsbook.API → MongoDB "Sportsbook" (Fixture/Market/Selection, secondary read pref)                          [PF]
  ↑  BaseResponseDTO<T> { StatusCode, Message, Result }                                                              [PF]
odds-engine-2 (UOF/Betradar feed) ── RabbitMQ r.oe.sb.engine.* ──▶ Mongo write + Websocket.API2 push               [OE]
```

---

## 1. Initial application load
**Browser → platform-web circuit → platform-final gateway (parallel fetches) → render.**

1. **Browser GET** any URL → platform-web Kestrel → middleware: `UseStaticFiles` →
   `MaintenanceMiddleware` (reads `clientId`, checks maintenance JSON + `CF-Connecting-IP` whitelist,
   may redirect to `/maintenance.html`) → `UseRouting` → `MapBlazorHub("/blazor", WebSockets only)`
   → `MapFallbackToPage("/_Host")`. **[PW]**
2. **`_Host.cshtml` SSR** (no circuit yet): extracts client IP/UA, resolves layout (AL vs EL2),
   loads operator CDN config (`CdnConfigurationProvider`), picks CSS/favicon, renders `App` with
   `ServerPrerendered` and a `ClientInfo` cascade. **[PW]**
3. **Blazor circuit**: `blazor.server.js` opens the `/blazor` WebSocket; a **scoped DI scope** is
   created → instantiates `IPlatformRepository`, `ApiUrlResolver`, the four state containers,
   `HubConnection`, `PreRenderService`, etc. A pre-render guard prevents API/JS calls during SSR. **[PW]**
4. **Page `OnInitializedAsync`** (e.g. `Index`): resolves theme, gets browser timezone offset (JS),
   builds `OperatorModel` from query params + `ClientInfo`, sets `OperatorState.Model`, registers
   `Unauthorized`/`InternalServerError` handlers, calls `RefreshHeader(isOGAuth)` to stamp HTTP
   headers, then **`Task.WhenAll`**: translations + sports tree + selector market templates,
   wallet balance, line-view session, announcements; then `SubscribeAsync()` (SignalR handlers +
   `StartAsync`), `GetBetConfig`, and (if authorized) favorites + boosted fixtures. **[PW]**
5. **Each repository call** → typed `HttpClient` (base `API:PlatformAPI`, 25 s timeout, 2-attempt
   retry keyed on `StatusCode`) → **platform-final `Platform.API`**. **[PW]**→**[PF]**
6. **platform-final gateway pipeline** runs (`ForwardedHeaders → Swagger → Routing → CORS →
   UseAuthorization(inert) → MapControllers`); the controller's **`APIKeyFilter`** validates
   `x-api-key` in `OnActionExecuting`; the action calls a downstream typed `HttpClient` repo
   (Sportsbook/Betting/Wallet) which reads **MongoDB**/**SQL** and returns `BaseResponseDTO<T>`. **[PF]**
7. **Response** deserialized (`System.Text.Json`, case-insensitive) → mapped to UI models → stored
   in state/instance fields → `PropertyChanged` → `InvokeAsync(StateHasChanged)` → Blazor diff →
   DOM patch over the circuit WebSocket. **[PW]**

> The browser never holds an API key or talks to platform-final directly — platform-web is a
> **server-side BFF**; all platform-final calls are server-to-server. **[PW]**

## 2. Fixture / market / sports-tree load
1. Triggered by `LoadTranslationSportsAndSelectorMarketTemplates()` (AL) / `Euro2Layout.LoadPage()`
   (EL2), and on scope change. **[PW]**
2. Calls `GetTranslations()`, `GetSelectorMarketTemplates()`, `GetSportsTree()`, then a scope-specific
   fixture call (`GetLiveFixtures`/`GetTodayFixtures`/`GetDailyFixtures`/`GetOutrightFixtures`/
   `GetFixturesByLeague`/…). Sports are filtered to those having a matching selector template. **[PW]**
3. Each maps to a `GET v1/sportsbook/...` route on **`Platform.API`** (see
   [PublicApiSurface.md](PublicApiSurface.md)). **[PF]**
4. The gateway delegates to **`Sportsbook.API`**, which queries **MongoDB `Sportsbook`** collections
   (`Fixture`/`Market`/`Selection`, secondary read preference; falls back to primary), applies
   per-client curation from SQL `SportsbookContext` (top fixtures/leagues, ordering), and returns
   projection DTOs. **[PF]**
5. Response → `List<LeagueFixtureModel>`/`List<SportModel>` → parent page fixture list → child
   `Fixture`/`Market`/`Selection` components via `[Parameter]`. Market display format applied via the
   user's odds-type selection. **[PW]**
6. Fixture details (single fixture) → `GET v1/sportsbook/fixturedetails` → Sportsbook.API → Mongo
   `Market`+`Selection` for that `FiId`. Virtual fixtures resolve `fixtureId` in two calls. **[PW]**/**[PF]**

## 3. Live odds update (real-time, the hot path)
This flow originates **upstream** and pushes **down** to the browser — the reverse direction.
1. **odds-engine-2** ingests the UOF/Betradar feed, recomputes odds/market state, and publishes
   `OnOddsChange` / `OnBetStopFixture` / `OnBetStopGlobal` on RabbitMQ `e.central` with keys
   `r.oe.sb.engine.*`. **[OE]**
2. The Mongo `Sportsbook` read model is updated, and the **`Websocket.API2`** `/platform` hub's
   `PublishOddsChange*` / `PublishBetStop*` methods fan the change to subscribed groups
   (fed externally — see [WebSocketContracts.md](WebSocketContracts.md)). **[PF]**
3. **platform-web `HubConnection`** receives `OnOddsChange` (`OddsChangeFixtureMessageModel`, MessagePack,
   abbreviated fields). The handler (`OnOddsChangeAsyncV2` in `BasePage`) finds the matching
   `FixtureModel` in the in-memory list, updates score/period/market status/selection odds, bumps a
   `Counter`, and calls `InvokeAsync(StateHasChanged)`. **[PW]**
4. Child components (`Fixture`/`Selection`/`BetRecommendationSelection`) re-render via parameter
   change; odds cells flash up/down for ~4 s. No HTTP round-trip, no page reload. **[PW]**

## 4. Translation load
1. `GetTranslations()` → `GET v1/sportsbook/translations` (30 s client cache) → gateway →
   Sportsbook.API → `TranslationContext` (`SportsbookDynamic`/`Static`) → `Dictionary<string,string>`. **[PW]**/**[PF]**
2. Stored in `TranslationState.Model`; `PropertyChanged` re-renders the layout; components read
   `TranslationState.GetValue(key)`. On language change, re-fetched. **[PW]**
3. Names also arrive **inline** on odds messages (`SpNaTr/LeNaTr/…`), so live updates carry their own
   per-language strings without a translation lookup. **[PF]**
4. platform-web has a static fallback dictionary if the call fails. **[PW]**

## 5. Authentication
platform-final has **no auth framework**; auth is header/API-key (see [Authentication.md](Authentication.md)).
1. **App-level:** platform-web stamps `x-api-key` (`Custom:ApiKey`) + `x-operator-token` +
   `x-client-id` + `x-language-code` on every call. **[PW]** platform-final's `APIKeyFilter`
   validates the api-key; `AuthorizationFilter` (ClientSportsbook) additionally resolves an **active
   client** (Guid→ClientCode, 24 h cached) and the language. **[PF]**
2. **End-user:** the operator token is opaque and validated **downstream** (Operator/Wallet);
   `103`/`Operator_Error 1001` → platform-web fires `Unauthorized` → redirects to login. **[PW]**/**[PF]**
3. **OG (Oriental Gaming):** `OGAuth.razor` validates an HMAC-MD5 signature over `x-og-*` params,
   then redirects into AL/EL2; for OG client-ids the gateway's `APIKeyFilter` parses `x-og-*` headers
   into `BaseController.OGParameters`. **[PW]**/**[PF]**

## 6. User navigation (scope / sport / fixture)
1. User clicks a sport/scope/league; the component raises an `EventCallback` to the parent page;
   the page disconnects the prior SignalR fixture channel, updates `PageState`/scope, and calls
   `NavigateTo` (no full reload — Blazor circuit persists). **[PW]**
2. The new scope triggers a fixture fetch (flow 2) and a new SignalR channel subscribe
   (`SubscribeToEndpoint`/`SubscribeToBetslip` with `{ClientId, ChannelKey}`,
   e.g. `sports:{sportId}:livefixtures`). **[PW]**/**[PF]**
3. On disposal, the page unsubscribes all hub channels and disposes the connection. **[PW]**

## 7. Bet placement (write path, the spine)
1. Betslip → `PlaceBet(BetRequestDTO)` → `POST v2/bet/placement` on `Platform.API`. **[PW]**/**[PF]**
2. Gateway → `Betting.API` validates (stake/odds/market/limits → `StatusCode`), runs the bet
   **provider** (MTS/GTS by `ChannelType`), persists `Bet`+`BetSelection` (SQL `BettingContext`),
   and publishes `OnBetPlacement` to RabbitMQ. **[PF]**
3. Async consumers act: `Placement.Servicev2` (provider dispatch), `BetInfo.Service`,
   `AgentReports.Service`, `BetForwarder.Service`, `Client.TopMarket`. **[PF]**
4. Result (`BaseResponseDTO<BetResponseDTO>`) returns synchronously; status updates later arrive via
   `OnBetStatus`/`OnCashOutChange`/`OnWalletChange` over the hub. **[PF]**/**[PW]**

## 8. Cashout
1. `CashOut(BetCashoutRequestDTO)` → `POST v1/bet/cashout` → Betting.API → `OnBetFullCashOut`/
   `OnBetPartialCashOut`. `CashOut.Service` processes (MTS SDK), writes `PartialCashoutAudit`. **[PF]**
2. Live cashout value is maintained by `CashOutStake.Servicev2` reacting to `OnOddsChange`, and
   pushed as `OnCashOutChange` to the betslip's `MyBetsCashoutButton`. Value may change between quote
   and confirm (`142`). **[PF]**/**[PW]**

## Error & resilience touchpoints
- **2-attempt retry** in platform-web's repository, keyed on business `StatusCode`; `Maintenance(205)`
  sets a maintenance flag and continues; `101/102/1000` retry then raise `InternalServerError`. **[PW]**
- platform-final has **no global exception handler** — controllers `try/catch` to `BaseResponseDTO`
  with `StatusCode.Error` (HTTP 400); unguarded exceptions surface as bare 500s. **[PF]** (see
  [MiddlewarePipeline.md](MiddlewarePipeline.md)).
- SignalR auto-reconnects (3×5 s, then full reload). **[PW]**

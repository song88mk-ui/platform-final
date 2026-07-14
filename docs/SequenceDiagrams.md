# Sequence Diagrams (End-to-End)

> Phase 4. Mermaid sequence diagrams for the marquee flows, spanning **browser → platform-web →
> platform-final → odds-engine-2**. Provenance tags: **[PW]** platform-web, **[PF]** platform-final,
> **[OE]** odds-engine-2. Narratives: [RequestLifecycle.md](RequestLifecycle.md).

Participants used across diagrams:
`Browser`, `PW` (platform-web Blazor circuit), `PWRepo` (IPlatformRepository), `PWHub` (HubConnection),
`Gateway` (platform-final `Platform.API`), `Filter` (APIKeyFilter), `SB` (Sportsbook.API),
`Bet` (Betting.API), `Mongo`, `SQL`, `MQ` (RabbitMQ e.central), `WS` (Websocket.API2 hub),
`OE` (odds-engine-2), `Settle` (Settlement.Service), `CashOut` (CashOut/CashOutStake services),
`Wallet` (Wallet.API).

---

## 1. Initial application load
```mermaid
sequenceDiagram
    actor Browser
    participant PW as platform-web (_Host/Index)
    participant PWRepo as IPlatformRepository
    participant PWHub as HubConnection
    participant Gateway as Platform.API
    participant Filter as APIKeyFilter
    participant SB as Sportsbook.API
    participant Mongo
    participant WS as Websocket.API2

    Browser->>PW: GET / (clientId, theme)
    Note over PW: StaticFiles → Maintenance → MapBlazorHub → _Host SSR
    PW-->>Browser: prerendered HTML + blazor.server.js
    Browser->>PW: open /blazor WebSocket (circuit + scoped DI)
    Note over PW: Index.OnInitializedAsync → set OperatorState, RefreshHeader
    par Parallel initial fetch (Task.WhenAll)
        PW->>PWRepo: GetTranslations / GetSportsTree / GetSelectorMarketTemplates
        PWRepo->>Gateway: GET v1/sportsbook/* (x-api-key, x-client-id, x-operator-token)
        Gateway->>Filter: OnActionExecuting (validate x-api-key)
        Filter-->>Gateway: ok
        Gateway->>SB: typed HttpClient
        SB->>Mongo: read Fixture/Market/Selection (secondary)
        Mongo-->>SB: docs
        SB-->>Gateway: BaseResponseDTO<T>
        Gateway-->>PWRepo: 200 BaseResponseDTO<T>
        PWRepo-->>PW: mapped models
    and Wallet / announcements / line-view
        PW->>PWRepo: GetWalletBalance (if authorized)
        PWRepo->>Gateway: GET v1/wallet/balance
    end
    PW->>PWHub: SubscribeAsync → StartAsync
    PWHub->>WS: connect /platform (MessagePack, WS only)
    WS-->>PWHub: connected (GetConnectionId)
    Note over PW: StateHasChanged → Blazor diff → DOM patch
    PW-->>Browser: rendered UI over circuit
```

## 2. Fixture list load + market render
```mermaid
sequenceDiagram
    participant PW as Page (scope=Live)
    participant PWRepo as IPlatformRepository
    participant Gateway as Platform.API
    participant SB as Sportsbook.API
    participant Mongo
    participant SQL as SportsbookContext (curation)

    PW->>PWRepo: GetLiveFixtures(sportId, selectorTemplate)
    PWRepo->>Gateway: GET v1/sportsbook/sports/livefixtures?sportId=
    Gateway->>SB: delegate
    SB->>Mongo: query Fixture/Market/Selection (FiStId live)
    SB->>SQL: apply client top/ordering/templates
    SQL-->>SB: curation
    Mongo-->>SB: catalogue docs
    SB-->>Gateway: BaseResponseDTO<FixtureResponseDTO>
    Gateway-->>PWRepo: 200
    PWRepo-->>PW: List<LeagueFixtureModel>
    Note over PW: fixtures → LeagueGroup → Fixture → Selection (odds in user's format)
```

## 3. Live odds update (push; reverse direction)
```mermaid
sequenceDiagram
    participant OE as odds-engine-2
    participant MQ as RabbitMQ e.central
    participant WS as Websocket.API2 hub
    participant PWHub as platform-web HubConnection
    participant PW as BasePage handler
    actor Browser

    OE->>MQ: publish OnOddsChange (r.oe.sb.engine.onoddschange)
    MQ->>WS: deliver (feeder → PublishOddsChangeForMainMarkets)
    WS-->>PWHub: OnOddsChange (OddsChangeFixtureMessageModel, MessagePack)
    Note over PW: OnOddsChangeAsyncV2 → find FixtureModel → update odds/score → Counter++
    PW->>PW: InvokeAsync(StateHasChanged)
    PW-->>Browser: odds cell re-render + up/down flash (~4s)
```

## 4. Bet placement (write path + async fan-out)
```mermaid
sequenceDiagram
    actor Browser
    participant PW as Betslip (Index)
    participant Gateway as Platform.API
    participant Bet as Betting.API
    participant Prov as BetProvider (MTS/GTS)
    participant SQL as BettingContext
    participant MQ as RabbitMQ
    participant Settle as Placement/BetInfo/AgentReports/...

    Browser->>PW: confirm bet
    PW->>Gateway: POST v2/bet/placement (BetRequestDTO)
    Gateway->>Bet: delegate (typed HttpClient)
    Bet->>Bet: validate stake/odds/market/limits → StatusCode
    Bet->>Prov: ValidateAsync (by ChannelType)
    Prov-->>Bet: Accepted / Rejected / Pending
    Bet->>SQL: insert Bet + BetSelection
    Bet->>MQ: publish OnBetPlacement
    MQ-->>Settle: fan-out (Placement.Servicev2, BetInfo, AgentReports, BetForwarder, TopMarket)
    Bet-->>Gateway: BaseResponseDTO<BetResponseDTO>
    Gateway-->>PW: 200 (Success) / 400 (StatusCode error)
    Note over PW: later OnBetStatus/OnWalletChange arrive via hub
```

## 5. Cashout (quote → confirm → settle)
```mermaid
sequenceDiagram
    participant OE as odds-engine-2
    participant CS as CashOutStake.Servicev2
    participant WS as Websocket.API2
    participant PW as MyBetsCashoutButton
    participant Gateway as Platform.API
    participant CashOut as CashOut.Service
    participant SQL as BettingContext

    OE-->>CS: OnOddsChange
    CS->>CS: recompute live cashout value
    CS->>WS: publish OnCashOutChange
    WS-->>PW: OnCashOutChange (new offer)
    PW->>Gateway: POST v1/bet/cashout (BetCashoutRequestDTO)
    Gateway->>CashOut: OnBetFullCashOut / OnBetPartialCashOut
    CashOut->>SQL: write PartialCashoutAudit; update Bet
    CashOut-->>Gateway: result (or 142 value changed → reconfirm)
    Gateway-->>PW: BaseResponseDTO<BetCashoutModel>
```

## 6. Settlement (async, server-side)
```mermaid
sequenceDiagram
    participant OE as odds-engine-2
    participant MQ as RabbitMQ
    participant Settle as Settlement.Service
    participant SQL as BettingContext
    participant Wallet as Wallet.API
    participant Reports as AgentReports.Service
    participant WS as Websocket.API2
    participant PW as platform-web

    OE->>MQ: OnBetSettlement
    MQ->>Settle: deliver (trace context in headers)
    Settle->>SQL: grade BetSelection → Bet status; ParlayBonusAudit; BetAudit
    Settle->>Wallet: credit winnings (if Won)
    Settle->>Reports: winloss
    Settle->>WS: OnBetStatus (r.bet.betstatus.s88)
    WS-->>PW: OnBetStatus / OnWalletChange
    PW-->>PW: update MyBets + wallet display
```

## 7. Authentication (API-key + OG)
```mermaid
sequenceDiagram
    actor Browser
    participant PW as platform-web
    participant Gateway as Platform.API
    participant Filter as APIKeyFilter / AuthorizationFilter
    participant SQL as PlatformContext (Client)

    Browser->>PW: launch (clientId [+ x-og-* for OG])
    Note over PW: OGAuth.razor validates HMAC-MD5 (OG only)
    PW->>Gateway: request + x-api-key + x-client-id + x-operator-token
    Gateway->>Filter: OnActionExecuting
    Filter->>Filter: x-api-key == Custom:APIKey ? else 101
    Filter->>SQL: resolve active ClientCode (24h cache)   %% ClientSportsbook flow
    SQL-->>Filter: ClientCode (or empty → 102)
    Filter-->>Gateway: ok / BadRequest(StatusCode)
    Note over Gateway: operator-token validated DOWNSTREAM → 103/1001 → PW redirects to login
```

## 8. Translation load + language switch
```mermaid
sequenceDiagram
    participant PW as platform-web
    participant Gateway as Platform.API
    participant SB as Sportsbook.API
    participant T as TranslationContext

    PW->>Gateway: GET v1/sportsbook/translations (x-language-code)
    Gateway->>SB: delegate
    SB->>T: SportsbookDynamic/Static by language + glossary version
    T-->>SB: rows
    SB-->>Gateway: BaseResponseDTO<List<TranslationResponseDTO>>
    Gateway-->>PW: 200 → Dictionary<string,string> (30s cache)
    Note over PW: TranslationState.Model set → PropertyChanged → re-render
    Note over PW: live odds messages also carry inline SpNaTr/LeNaTr/... (no extra call)
```

> Rendering: see [RenderingPipeline.md](RenderingPipeline.md). State propagation:
> [StateFlow.md](StateFlow.md). Per-component data: [ComponentDataFlow.md](ComponentDataFlow.md).
> If your Markdown viewer doesn't render Mermaid, the fenced ```mermaid blocks are plain text.

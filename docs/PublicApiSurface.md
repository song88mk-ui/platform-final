# Public API Surface — `Platform.API` Gateway

> Added during continued Phase 1, cross-referenced against the **platform-web** sibling
> (the Blazor front-end / BFF that consumes this gateway) whose `docs/` provided verified
> request/response contracts. Routes here are **[Verified]** directly in platform-final source
> (`Platform.API/Controllers/*.cs`); DTO field shapes are **[Verified: consumer]** from
> platform-web source. See [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## Why this matters
`Platform.API` is the **single public entry point** for player-facing clients (platform-web,
Platform.Parent.Web, and operator integrations). It fans out internally over HTTP to
`Betting.API`, `Sportsbook.API`, `Wallet.API`, `Operator.API`, and the casino (QTech) API, and
over RabbitMQ (via `RabbitMQ.API`). Two controllers split the surface by audience:

| Controller | Filter | Audience | Route count |
|------------|--------|----------|------------:|
| `IntegrationController` | `APIKeyFilter` (`x-api-key`) | public/player clients | ~62 |
| `InternalController` | `InternalAPIKeyFilter` | back office / config / settlement ops | ~34 |

Class attributes: `[ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]`.
**[Verified]** `Platform.API/Controllers/IntegrationController.cs:19`.

## Standard request headers (consumer contract) `[Verified: consumer]`
platform-web stamps these on every gateway call:
`x-api-key`, `x-operator-token`, `x-client-id`, `x-language-code`, `x-is-ssr`,
`x-country-code`, `x-user-ip`, `x-referer`, `user-agent`; plus `x-og-*` (Oriental Gaming)
headers when OG auth is active. The internal key variant uses `Custom:InternalApiKey`.

## Response envelope `[Verified]`
All responses: `BaseResponseDTO<T>` { `StatusCode` (business enum), `Message`, `Result` }.
Consumers branch on `StatusCode`, **not** HTTP status (success/maintenance → 200, else 400).

---

## IntegrationController — public endpoints `[Verified]`
Cited line numbers are in `Platform.API/Controllers/IntegrationController.cs`.

### Authentication & wallet
| Verb | Route | Line | Notes |
|------|-------|-----:|-------|
| POST | `v1/authentication/login` | 32 | operator-token login |
| POST | `v1/authentication/logout` | 82 | |
| GET | `v1/authentication/og` | 2604 | Oriental Gaming auth/token |
| GET | `v1/wallet/balance` | 124 | `WalletBalanceResponseDTO` (UserId, Balance, Payable, CurrencyCode, RefCurrencyCode) |

### Betting
| Verb | Route | Line | Notes |
|------|-------|-----:|-------|
| POST | `v2/bet/placement` | 164 | body `BetRequestDTO` (Stake, OddsType, Channel, Selections[], …) |
| POST | `v1/bet/maxstake` | 262 | body `BetRequestDTO` → `MaxStakeResponseDTO.Stake` |
| GET | `v1/bet/config` | 310 | `BetConfigResponseDTO` (sport/parlay/tax/priceboost/betbuilder configs) |
| GET | `v1/bet/status` | 350 | by `BetId` → `BetStatus` int |
| GET | `v1/bet/history` | 409 | query: BetId/dates/page/filter |
| POST | `v2/bet/history` | 461 | body `BetHistoryRequestDTO` (paged, filters) |
| GET | `v1/bet/history/settled` | 513 | |
| GET | `v1/bet/history/running` | 553 | |
| POST | `v1/bet/cashout` | 597 | body `BetCashoutRequestDTO` (BetId, Channel, CashoutStake, CashoutStakePercent?) |

### Sportsbook — catalogue & fixtures
| Verb | Route | Line |
|------|-------|-----:|
| GET | `v1/sportsbook/sports` | 1942 |
| GET | `v1/sportsbook/sports/topleagues` | 694 |
| GET | `v1/sportsbook/sports/allleagues` | 735 |
| GET | `v1/sportsbook/sports/livefixtures` | 1279 |
| GET | `v1/sportsbook/sports/todayfixtures` | 1373 |
| GET | `v1/sportsbook/sports/upcomingfixtures` | 1469 |
| GET | `v1/sportsbook/sports/dailyfixtures` | 1565 |
| GET | `v1/sportsbook/sports/outrightfixtures` | 1663 |
| GET | `v1/sportsbook/sports/topleaguefixtures` | 1757 |
| GET | `v1/sportsbook/sports/segments/fixtures` | 1803 |
| GET | `v1/sportsbook/sports/segments/leagues/fixtures` | 1850 |
| GET | `v1/sportsbook/sports/segments/leagues/{livefixtures,todayfixtures,upcomingfixtures,dailyfixtures,outrightfixtures}` | 1325/1420/1516/1613/1709 |
| GET | `v1/sportsbook/livefixturecount` / `todayfixturecount` | 1194 / 1235 |
| GET | `v2/sportsbook/topfixtures` / `v2/sportsbook/topfixtures/fixtureId` | 1065 / 776 |
| GET | `v1/sportsbook/activefixtures` / `.../details` | 1108 / 1151 |
| GET | `v1/sportsbook/fixturedetails` | 1994 |
| GET | `v1/sportsbook/selectiondetails` | 2040 |
| GET | `v1/sportsbook/search` | 1898 |

### Sportsbook — markets, favorites, content
| Verb | Route | Line |
|------|-------|-----:|
| GET | `v1/sportsbook/markettemplategroups` | 817 |
| GET | `v1/sportsbook/selectormarkettemplates` | 857 |
| GET | `v1/sportsbook/marketsortorder` / `fixturesortorder` | 897 / 937 |
| GET/POST/DELETE | `v1/sportsbook/userfavorites` | 1021 / 2088 / 2130 |
| GET | `v1/sportsbook/userfavorites/fixtures` | 977 |
| GET | `v1/sportsbook/boostedfixtures` | 2173 |
| POST | `v1/sportsbook/betbuilder/markettemplates` | 2218 |
| POST | `v1/sportsbook/betbuilder/selections/odds` | 2259 |
| GET | `v1/sportsbook/client/markettemplateplayerprops` | 2299 |
| GET | `v1/sportsbook/livestream` | 645 |
| GET | `v1/sportsbook/translations` | 2489 |
| GET | `v1/sportsbook/highlights` / `highlights/fixtures` | 2525 / 2564 |
| GET | `v1/sportsbook/betrecommendations` | 2638 |
| GET | `v1/sportsbook/announcements` | 2672 |
| GET | `v1/sportsbook/ccf` | 2711 |

### Casino / eSport
| Verb | Route | Line |
|------|-------|-----:|
| GET | `v1/casino` / `v1/casino/live` / `v1/casino/virtualgames` | 2340 / 2378 / 2416 |
| GET | `v1/esport/config` | 2454 |

---

## InternalController — back-office/ops endpoints `[Verified]`
Cited line numbers in `Platform.API/Controllers/InternalController.cs`. Behind `InternalAPIKeyFilter`.

- **Client/config:** `POST v1/client` (37), `GET v1/client` (460), `GET v1/clients` (468),
  `POST v1/client/status` (52), `POST v1/config/client` (294), `DELETE v1/cache/client/{clientId}` (22),
  `POST v1/cachepreload` (445).
- **Top fixtures/leagues:** `…/topfixtures(/list|/copy|/sportstree)` (62/113/128/95),
  `GET v2/client/topfixtures` (77), `…/topleagues(/list|/copy)` (144/159/174/189).
- **Catalogue (BO):** `GET v1/client/sports` (204), `v1/client/allleagues` (219),
  `v2/client/allleagues` (241), `v1/client/sportgroups` (429),
  `v1/client/exchangerate(s)` (264/279).
- **Settlement/cancel:** `POST v1/bet/cancel` (309), `v1/bet/manualsettlement` (324),
  `v1/bet/details` (339), `v1/bet/resendsettlement` (354), `v1/bet/resendprovidercancellation` (369), `v1/bet/resendsettlement/details` (384).
- **Parlay bonus:** `POST/GET v1/client/parlaybonus` (399/414).
- **Announcements:** `POST v1/client/announcements(/post|/delete)` (485/500/515).
- **Translation:** `POST v1/translation/recovery` (476).

---

## StatusCode enum (business result codes) `[Verified]`
`Platform.Shared/Enums/StatusCode.cs` (108 lines). Selected — values match what platform-web
decodes (a few have consumer-side aliases noted):

| Code | Name | Code | Name |
|-----:|------|-----:|------|
| 100 | Success | 126 | **Odds_Or_Lines_Have_Changed** (consumer alias `Odds_Is_Stale`) |
| 101 | Missing_Or_Unsupported_APIKey_In_Request_Headers | 128 | Odds_Is_Inactive |
| 102 | Missing_Or_Unsupported_ClientId_In_Request_Headers | 129 | **Market_Has_Been_Suspended** (alias `Betstop_On_Fixture`) |
| 103 | Missing_Or_Unsupported_OperatorToken_In_Request_Headers | 130 | Betstop_On_Global |
| 104 | Insufficient_Funds | 131 | Max_Liability_By_Tier_Not_Found |
| 105–111 | Unsupported_* / CurrencyCode_Mismatch | 132 | Mts_Error |
| 112–117 | Odds/Stake/Payout validation | 139 | No_Record_Found |
| 118–121 | Betting_Restricted (sport/segment/league) | 140 | Not_Authorized |
| 122 | IPAddress_Must_Be_IPv4 | 141 | CashOut_Is_Unavailable |
| 123 | Fixture_Has_No_Livestream | 142 | CashOut_Stake_Changed_From_X_To_Y |
| 124 | Market_Is_Suspended | 143 | Bet_Not_Found |
| 125 | Market_Is_Not_Offered | 144 | Parlay_Bonus_Not_Updated |
| 145–153 | Parlay/boost/livestream/resend/date rules | 154–157 | Bet_Builder rules (2–10 selections, same fixture, no-boost) |
| 205 | Maintenance | 1000 | Error |
| | | 1001 | Operator_Error |

Consumer retry behavior (platform-web): 101/102/1000 → retry once then `InternalServerError`;
103/1001 → `Unauthorized` (redirect); 205 → set maintenance + continue. `[Verified: consumer]`

## Internal fan-out (who serves what) `[Likely]`
The gateway delegates: sportsbook reads → `Sportsbook.API`; bet ops → `Betting.API`;
wallet → `Wallet.API`; casino → QTech API; auth → `Operator.API`/`Auth`. Confirmed by the
typed `HttpClient` repos in `Platform.API/Startup.cs`; exact per-route mapping not traced.

## Related real-time surface
Player real-time updates do **not** come from this REST gateway — they arrive over SignalR from
the WebSocket hub (`websocket-api.*/platform`). See [WebSocketContracts.md](WebSocketContracts.md).

# Platform API Endpoints (Quick Reference)

> Phase 9. Fast lookup of the `Platform.API` gateway surface. **Authoritative + line-cited:**
> [PublicApiSurface.md](../PublicApiSurface.md) (verified in `Platform.API/Controllers/*`).
> Consumer-side contract detail (DTO fields, retry): [PlatformApi.md](../PlatformApi.md). All **[Verified]**.

## Audiences & keys
| Controller | Filter | Key | Consumer |
|------------|--------|-----|----------|
| `IntegrationController` (~62 routes) | `APIKeyFilter` | `Custom:APIKey`/`WebSocketAPIKey` | platform-web (players) |
| `InternalController` (~33 routes) | `InternalAPIKeyFilter` | `Custom:InternalAPIKey` | backoffice-final |

Header set: `x-api-key`, `x-operator-token`, `x-client-id`, `x-language-code`, `x-country-code`,
`x-user-ip`, `x-og-*` (OG). Envelope: `BaseResponseDTO<T>`; success/maintenance→200 else 400.

## Public endpoints — by area (path only; verbs/lines in PublicApiSurface.md)
**Auth/wallet:** `v1/authentication/login|logout|og`, `v1/wallet/balance`.
**Betting:** `v2/bet/placement`, `v1/bet/maxstake|config|status|cashout`, `v1/bet/history(/settled|/running)`, `v2/bet/history`.
**Sportsbook fixtures:** `v1/sportsbook/sports`, `.../sports/{top leagues,allleagues,livefixtures,todayfixtures,upcomingfixtures,dailyfixtures,outrightfixtures,topleaguefixtures,segments/fixtures,segments/leagues/fixtures}`, `.../segments/leagues/{livefixtures,todayfixtures,upcomingfixtures,dailyfixtures,outrightfixtures}`, `v1/sportsbook/{live,today}fixturecount`, `v2/sportsbook/topfixtures(/fixtureId)`, `v1/sportsbook/activefixtures(/details)`, `v1/sportsbook/fixturedetails`, `v1/sportsbook/selectiondetails`, `v1/sportsbook/search`.
**Markets/content:** `v1/sportsbook/{markettemplategroups,selectormarkettemplates,marketsortorder,fixturesortorder}`, `userfavorites(/fixtures)` (GET/POST/DELETE), `boostedfixtures`, `betbuilder/markettemplates`, `betbuilder/selections/odds`, `client/markettemplateplayerprops`, `livestream`, `translations`, `highlights(/fixtures)`, `betrecommendations`, `announcements`, `ccf`.
**Casino/eSport:** `v1/casino(/live|/virtualgames)`, `v1/esport/config`.

## Internal endpoints — by area
**Client/config:** `POST/GET v1/client`, `v1/clients`, `v1/client/status`, `v1/config/client`,
`DELETE v1/cache/client/{clientId}`, `v1/cachepreload`.
**Curation:** `v1/client/topfixtures(/list|/copy|/sportstree)`, `v2/client/topfixtures`,
`v1/client/topleagues(/list|/copy)`; `v1/client/{sports,allleagues,sportgroups,exchangerate(s)}`;
`v2/client/allleagues`.
**Settlement/cancel:** `v1/bet/{cancel,manualsettlement,details,resendsettlement,resendprovidercancellation,resendsettlement/details}`.
**Parlay bonus:** `POST/GET v1/client/parlaybonus`. **Announcements:** `v1/client/announcements(/post|/delete)`.
**Translation:** `v1/translation/recovery`.

## Other API surfaces (not the gateway)
- **`ClientSportsbook.API`** (`Custom:ClientSportsbookAPI`): `v1/sportsbook/activefixtures(/details/{id})`,
  `market/subscribe|unsubscribe|subscription`.
- **`Operator.API/OddsFeedController`**: receives oddsfeed callbacks `/bet/status/placement|cancellation`.
- **`RabbitMQ.API`**: `POST v1/broker/publish`. **Wallet.API**: `v1/credit|debit|userwallettransfer`, `GET v1/userwallet`.
- Partner inbound: QTech (`v1/transactions`, …), RedCat (`api/redcat/*`, `v1/vs/*`), Oddin (`v1/userdetails|debituser|credituser`).

## StatusCode quick map `[Verified]`
100 Success · 101–103 missing key/client/operator-token · 104 Insufficient_Funds · 112–117 odds/stake/
payout · 118–121 betting restricted · 124–130 market/odds/bet-stop · 132 Mts_Error · 141–142 cashout ·
145–149 parlay/boost · 154–157 bet-builder · 205 Maintenance · 1000 Error · 1001 Operator_Error.
Full list: [PublicApiSurface.md](../PublicApiSurface.md).

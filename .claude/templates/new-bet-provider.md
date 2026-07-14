# Template: New Bet Provider

Add a betting/validation provider via the factory — **no call-site `if`/`switch` on channel**.

## 1. Add a `ChannelType` (`Platform.Shared/Enums/ChannelType.cs` — currently `BG=0,EM=1,OD=2,SR=3`).
⚠️ Enum values are a contract used for routing — appending is safer than renumbering (R-2).

## 2. Implement `IBetProvider` (`Platform.Shared/Providers/<Name>/<Name>BetProvider.cs`)
```csharp
public class XxxBetProvider : IBetProvider {
    public ChannelType ChannelType => ChannelType.Xxx;
    public Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken ct) { ... }
    public Task<bool> IsAvailableAsync(BetRequestDTO request) { ... }
    public Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request) { ... }
    public void ProcessSettlement(OnBetSettlement bet, string correlationId) { ... }
    public void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger=false) { ... }
}
```
- External calls go through a typed `HttpClient` repo (mirror `GTSRepository`/`MtsBetProvider`).
- Gate with a `Custom:*` feature flag (e.g. `IsProviderEnabled`).

## 3. Register in the provider extension method (`AddBettingProviders`) so `IBetProviderFactory.Resolve`
   picks it up by `ChannelType`. Add any auth/HTTP repos there too.

## 4. Config: base URL (`API:*`), creds (`Custom:*` — via SSM, never committed). `/update-docs`
   ([docs/SharedLibraries.md](../../docs/SharedLibraries.md) §3, [docs/OddsEngineIntegration.md](../../docs/OddsEngineIntegration.md)).

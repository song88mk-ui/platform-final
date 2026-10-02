# Mutation testing (Stryker.NET)

Phase B of the test plan. Mutation testing measures test *quality*: Stryker edits the production
code (a mutant), re-runs the suite, and records whether any test noticed. A surviving mutant is code
you can break without a single test failing.

Line coverage says a line ran. Mutation score says the line's behaviour is actually asserted. That
distinction is why this exists: `Platform.Shared.Test` reports 68.71% line coverage but only 23.70%
branch coverage, and a large share of the line figure comes from a reflective sweep over DTOs that
asserts almost nothing.

## Running it

Stryker is a .NET global tool, not a package reference, so it is not restored by `dotnet restore`:

```bash
dotnet tool install --global dotnet-stryker
```

Install note: the repository's NuGet sources include the private `github` feed, which stalls the
install when `NUGET_PASSWORD` is unset. Point the install at nuget.org only if that happens:

```bash
dotnet tool install --global dotnet-stryker --configfile <a nuget.config listing only nuget.org>
```

Then, from this directory:

```bash
cd Platform/Platform.Shared.Test
dotnet-stryker --config-file stryker-config.json
```

A run takes about 40 seconds and writes an HTML and a JSON report under `StrykerOutput/`, which is
gitignored. Open the HTML report to see each surviving mutant in context.

## Scope, and why it is narrow

Stryker reruns the test suite once per surviving mutant, so it is only viable where the tests
are fast and fully in-memory. `stryker-config.json` mutates `Platform.Shared/Extensions`,
`Helpers`, `Providers`, and four files in `Utility` - the code whose tests touch no SQL Server,
RabbitMQ or MongoDB.

**Do not widen `mutate` to a project whose tests need infrastructure.** Such a run appears to
hang rather than fail, because every mutant waits on a connection that never arrives.

Excluded deliberately:

| Excluded | Reason |
|---|---|
| `Providers/MTS/**` | MTS is retired. It still has DI registrations and answers `ChannelType.SR`, which no call site resolves, so it should not be defended by a threshold |
| `Utility/MemCache.cs` | wraps `MemoryCache.Default`, a per-pod static |
| `Utility/MessageBroker.cs` | needs a live RabbitMQ connection |
| `Extensions/BetAuditEntityExtension.cs` | takes a `DbSet<T>`; needs the EF seam from Tier 6 |
| `Extensions/BetSelectionAuditEntityExtension.cs` | same |

`ignore-methods` excludes mutants inside `ILogger` calls. Without it, 83 of the provider
mutants were string and statement edits to log messages: killing those would mean asserting log
text, which is brittle and says nothing about behaviour. It is noise removal, not score
inflation - the reason codes that callers actually branch on are ordinary strings elsewhere in
the method and are still mutated.

Stryker also cannot tell whether a log call's arguments are safe. Removing `.Mask()` from a
secret passed to `LogError` produces no mutant, so that is tested directly instead
(`Platform.Operator.Servicev2.Test/Repository/BettingRepositoryOGKeyLoggingTests.cs`, GM-880).

## Baseline (2026-09-10, after GM-807)

**Overall score 83.51%** - 395 killed, 55 survived, 23 not covered by any test, out of 473
mutants. Run takes about 40 seconds.

History, all measured: 53.19% at DEVOPS-365 over 282 mutants, 87.23% after GM-804 added tests
for `Helper` and `ObjectExtensions`, 90.35% after GM-806 deleted the unreachable guards, then
**83.51% after GM-807 brought `Providers` into scope**. That last step lowered the number by
widening what is measured, from 259 mutants to 473. A drop after widening the scope is the gate
reaching further, not a regression - read the mutant count alongside the percentage.

| File | Killed | Survived | No coverage | Score |
|---|---|---|---|---|
| `OddsTypeHelper.cs` | 29 | 0 | 0 | 100.0% |
| `StringExtensions.cs` | 27 | 0 | 0 | 100.0% |
| `BetProviderFactory.cs` | 6 | 0 | 0 | 100.0% |
| `LiveStreamFactory.cs` | 6 | 0 | 0 | 100.0% |
| `ListExtensions.cs` | 5 | 0 | 0 | 100.0% |
| `Formatting.cs` | 4 | 0 | 0 | 100.0% |
| `JwtParser.cs` | 4 | 0 | 0 | 100.0% |
| `HashExtensions.cs` | 3 | 0 | 0 | 100.0% |
| `DateTimeExtensions.cs` | 2 | 0 | 0 | 100.0% |
| `EnumExtensions.cs` | 1 | 0 | 0 | 100.0% |
| `DecimalJsonConverter.cs` | 11 | 1 | 0 | 91.7% |
| `BetGeniusLiveStream.cs` | 21 | 1 | 1 | 91.3% |
| `ObjectExtensions.cs` | 41 | 4 | 0 | 91.1% |
| `Helper.cs` | 80 | 12 | 0 | 87.0% |
| `DecimalExtensions.cs` | 5 | 0 | 1 | 83.3% |
| `LocalizedSearchMatcher.cs` | 21 | 3 | 2 | 80.8% |
| `GTSBetProvider.cs` | 128 | 32 | 19 | 71.5% |
| `IntExtensions.cs` | 1 | 2 | 0 | 33.3% |

Thresholds are set from this measurement: `break` 80 (below the current score, so a run fails
only on a regression), `low` 82, `high` 92.

## Why the score is not higher

Two different reasons, and they should not be conflated.

**Equivalent mutants** - edits that cannot change observable behaviour, so no test can kill
them. The bet-builder path in `Helper.GetPayout` returns `trueStake` from its Void check, its
Cancelled check and its catch-all, so negating any of those conditions changes nothing. Both
`GetVoidFactor` overloads return `Zero` from the explicit branch and the fall-through.
`GetInstanceName` subtracts `'0'` then tests parity, and 48 is even. `First()` to
`FirstOrDefault()` survives wherever the collection is provably non-empty. In `GTSBetProvider`
the virtual-sport guards at lines 49 and 175 are unreachable from tests because
`IntExtensions.IsVirtualSportId` is still an empty stub, so both arms behave identically.

**Genuine gaps in `GTSBetProvider`'s cashout region** (lines 205-378). `GTSBetProviderCashoutTests`
covers the cashout paths behaviourally, but does not assert the test-prefix routing or the
`SendBet` status handling within them, so those mutants survive. That is a real, addressable
gap and the obvious next increment for this file.

`IntExtensions.cs:13,18` survive as the deliberate empty sport-ID stubs that
`IntExtensionsTests` records. `ObjectExtensions.cs:45` survives as a redundant but worthwhile
fast path.

## Not wired into CI

Deliberately manual or nightly, never a pull-request gate. CI already builds all 26 matrix images on
every push with no affected-files detection; adding a mutation run to that path would compound it for
little benefit. Run it when changing the code in scope, and after adding tests to it.

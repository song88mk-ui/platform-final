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

Stryker re-runs the test suite once per surviving mutant, so it is only viable where the tests are
fast and fully in-memory. `stryker-config.json` therefore mutates only
`Platform.Shared/Extensions`, `Helpers`, and four files in `Utility` - the code whose tests touch no
SQL Server, RabbitMQ or MongoDB.

**Do not widen `mutate` to a project whose tests need infrastructure.** Such a run appears to hang
rather than fail, because every mutant waits on a connection that never arrives.

Excluded deliberately:

| Excluded | Reason |
|---|---|
| `Utility/MemCache.cs` | wraps `MemoryCache.Default`, a per-pod static |
| `Utility/MessageBroker.cs` | needs a live RabbitMQ connection |
| `Extensions/BetAuditEntityExtension.cs` | takes a `DbSet<T>`; needs the EF seam from Tier 6 |
| `Extensions/BetSelectionAuditEntityExtension.cs` | same |

## Baseline (2026-09-10, after GM-806)

**Overall score 90.35%** - 234 killed, 22 survived, 3 not covered by any test, out of 259
mutants. Run takes about 40 seconds.

History: 53.19% at DEVOPS-365, 87.23% after GM-804 added 99 tests for `Helper` and
`ObjectExtensions`, 90.35% after GM-806 deleted the unreachable guards in
`StringExtensions` and `HashExtensions`. That last step raised the score by removing
mutants from the denominator (282 to 259), not by killing more.

| File | Killed | Survived | No coverage | Score |
|---|---|---|---|---|
| `OddsTypeHelper.cs` | 29 | 0 | 0 | 100.0% |
| `StringExtensions.cs` | 27 | 0 | 0 | 100.0% |
| `ListExtensions.cs` | 5 | 0 | 0 | 100.0% |
| `Formatting.cs` | 4 | 0 | 0 | 100.0% |
| `JwtParser.cs` | 4 | 0 | 0 | 100.0% |
| `HashExtensions.cs` | 3 | 0 | 0 | 100.0% |
| `DateTimeExtensions.cs` | 2 | 0 | 0 | 100.0% |
| `EnumExtensions.cs` | 1 | 0 | 0 | 100.0% |
| `DecimalJsonConverter.cs` | 11 | 1 | 0 | 91.7% |
| `ObjectExtensions.cs` | 41 | 4 | 0 | 91.1% |
| `Helper.cs` | 80 | 12 | 0 | 87.0% |
| `DecimalExtensions.cs` | 5 | 0 | 1 | 83.3% |
| `LocalizedSearchMatcher.cs` | 21 | 3 | 2 | 80.8% |
| `IntExtensions.cs` | 1 | 2 | 0 | 33.3% |

Thresholds are set from this measurement: `break` 85 (below the current score, so a run
fails only on a regression), `low` 88, `high` 95.

## Why the score is not 100%

Do not read the remaining 25 unkilled mutants as 25 missing tests. Nearly all are
**equivalent mutants** - edits that cannot change observable behaviour, so no test can kill
them. Two groups remain now that GM-806 has removed the dead code.

**Redundant branches that return the same value (`Helper.cs`, 12 mutants).** The
bet-builder path checks `Any(Void)`, then `Any(Cancelled)`, then falls through to a
catch-all, and all three return `trueStake`, so negating any of those conditions changes
nothing. Both `GetVoidFactor` overloads return `Zero` from their explicit `Won || Lost`
branch and from the fall-through. `GetInstanceName` subtracts `'0'` from a character and
then tests parity; since 48 is even, adding it gives the same parity.

**Guards on conditions that cannot occur.** `First()` to `FirstOrDefault()` survives
wherever the collection is already known non-empty - `string.Split` never returns an empty
array, and `GetPayout`'s branch 1 is guarded by `Count == 1`. Same for
`LocalizedSearchMatcher.cs:55,56`, where a name without a "vs." separator makes the away
participant equal the home one, so the two code paths agree.

`IntExtensions.cs:13,18` also survive, as the deliberate empty sport-ID stubs that
`IntExtensionsTests` records. `ObjectExtensions.cs:45` survives as a fast path that is
redundant but worth keeping: a primitive falls through the complex-object branch unchanged,
so deleting the check would be slower and less obvious.

Chasing a higher number from here means changing production code for no behavioural gain.
Treat 90% as the working ceiling for this scope.

## Not wired into CI

Deliberately manual or nightly, never a pull-request gate. CI already builds all 26 matrix images on
every push with no affected-files detection; adding a mutation run to that path would compound it for
little benefit. Run it when changing the code in scope, and after adding tests to it.

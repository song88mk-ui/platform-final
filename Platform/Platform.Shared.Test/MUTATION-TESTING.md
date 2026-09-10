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

## Baseline (2026-09-10, on the Phase A test suite merged with main)

**Overall score 53.19%** - 150 killed, 22 survived, 110 not covered by any test, out of 282 mutants.
Measured on covered code only, the score is **87.2%**.

Both numbers matter. 87.2% says the tests that exist are sharp. 53.19% says a third of the mutants
sit in code no test reaches at all.

| File | Killed | Survived | No coverage | Score |
|---|---|---|---|---|
| `OddsTypeHelper.cs` | 29 | 0 | 0 | 100.0% |
| `DateTimeExtensions.cs` | 2 | 0 | 0 | 100.0% |
| `EnumExtensions.cs` | 1 | 0 | 0 | 100.0% |
| `ListExtensions.cs` | 5 | 0 | 0 | 100.0% |
| `Formatting.cs` | 4 | 0 | 0 | 100.0% |
| `JwtParser.cs` | 4 | 0 | 0 | 100.0% |
| `DecimalExtensions.cs` | 5 | 0 | 1 | 83.3% |
| `LocalizedSearchMatcher.cs` | 21 | 3 | 2 | 80.8% |
| `StringExtensions.cs` | 39 | 6 | 4 | 79.6% |
| `DecimalJsonConverter.cs` | 9 | 3 | 0 | 75.0% |
| `HashExtensions.cs` | 3 | 1 | 0 | 75.0% |
| `ObjectExtensions.cs` | 18 | 7 | 20 | 40.0% |
| `IntExtensions.cs` | 1 | 2 | 0 | 33.3% |
| `Helper.cs` | 9 | 0 | 83 | 9.8% |

Thresholds in the config are set from this baseline, not picked in advance: `break` 45 (below the
current score, so a run fails only on a real regression), `low` 55, `high` 80. Raise them as the
score improves.

## What the baseline found

`Helper.cs` dominates the gap: 83 of its 92 mutants are unreached, because `HelperTests` covers only
`Helper.GetStake`. `ObjectExtensions.cs` is second at 20 unreached plus 7 survivors, despite having
the largest existing test file - line coverage there was hiding untested behaviour.

Several survivors are **not test gaps but unreachable code**, which is the more interesting result:

- `StringExtensions.cs:17,18` - after the `ogParam.Length <= 2` guard returns null, `Length >= 2` can
  never be false, so both ternaries are dead. Mutating them to `true` changes nothing.
- `StringExtensions.cs:56,59` - `result.Length <= 0` can never hold: `string.Split` always returns at
  least one element. Dead guard.
- `HashExtensions.cs:11` - `var hex = string.Empty;` is always overwritten before use. Dead
  initialiser.
- `ObjectExtensions.cs:41` - an always-true null-or-empty guard.
- `IntExtensions.cs:13,18` - block removal survives because both sport-ID lists are still empty
  stubs, which `IntExtensionsTests` already records.

Genuine test gaps among the survivors:

- `LocalizedSearchMatcher.cs:55,56` - `FirstOrDefault()` to `First()` and `Count > 1` to `>= 1` both
  survive, so the single-participant case is unasserted.
- `DecimalJsonConverter.cs:12,29` - `CanConvert` returning `false` survives, and the write path's
  output is not asserted.

## Not wired into CI

Deliberately manual or nightly, never a pull-request gate. CI already builds all 26 matrix images on
every push with no affected-files detection; adding a mutation run to that path would compound it for
little benefit. Run it when changing the code in scope, and after adding tests to it.

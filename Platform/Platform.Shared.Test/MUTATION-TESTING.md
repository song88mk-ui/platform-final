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

## Baseline (2026-09-10, after GM-804)

**Overall score 87.23%** - 246 killed, 29 survived, 7 not covered by any test, out of 282
mutants. Run takes about 40 seconds.

GM-804 raised this from the 53.19% first measured under DEVOPS-365, by adding 99 tests for
`Helper` and `ObjectExtensions`. Unreached mutants fell from 110 to 7.

| File | Killed | Survived | No coverage | Score |
|---|---|---|---|---|
| `OddsTypeHelper.cs` | 29 | 0 | 0 | 100.0% |
| `DateTimeExtensions.cs` | 2 | 0 | 0 | 100.0% |
| `EnumExtensions.cs` | 1 | 0 | 0 | 100.0% |
| `ListExtensions.cs` | 5 | 0 | 0 | 100.0% |
| `Formatting.cs` | 4 | 0 | 0 | 100.0% |
| `JwtParser.cs` | 4 | 0 | 0 | 100.0% |
| `DecimalJsonConverter.cs` | 11 | 1 | 0 | 91.7% |
| `ObjectExtensions.cs` | 41 | 4 | 0 | 91.1% |
| `Helper.cs` | 80 | 12 | 0 | 87.0% |
| `DecimalExtensions.cs` | 5 | 0 | 1 | 83.3% |
| `LocalizedSearchMatcher.cs` | 21 | 3 | 2 | 80.8% |
| `StringExtensions.cs` | 39 | 6 | 4 | 79.6% |
| `HashExtensions.cs` | 3 | 1 | 0 | 75.0% |
| `IntExtensions.cs` | 1 | 2 | 0 | 33.3% |

Thresholds are set from this measurement: `break` 80 (below the current score, so a run
fails only on a regression), `low` 85, `high` 90.

## Why the score is not higher

Do not read the remaining 36 unkilled mutants as 36 missing tests. Nearly all of them are
**equivalent mutants** - edits that cannot change observable behaviour, so no test can ever
kill them. They fall into three groups.

**Redundant branches that return the same value (Helper.cs).** The bet-builder path checks
`Any(Void)`, then `Any(Cancelled)`, then falls through to a catch-all, and all three return
`trueStake`. Negating any of those conditions changes nothing. Likewise both `GetVoidFactor`
overloads return `Zero` from their explicit `Won || Lost` branch and from the fall-through,
so `||` to `&&` is invisible. `GetInstanceName` subtracts `'0'` from a character and then
tests parity; since 48 is even, subtracting or adding it gives the same parity.

**Guards on conditions that cannot occur.** `First()` to `FirstOrDefault()` survives wherever
the collection is already known non-empty - `string.Split` never returns an empty array, and
`GetPayout`'s branch 1 is guarded by `Count == 1`.

**Dead code.** Four sites are unreachable and can only be cleared by deleting them, which is
a shared-kernel change and out of scope for GM-804:

- `StringExtensions.cs:17,18` - after the `Length <= 2` guard returns null, `Length >= 2` is
  always true, so both ternaries are dead
- `StringExtensions.cs:56,59` - `result.Length <= 0` can never hold
- `HashExtensions.cs:11` - the `string.Empty` initialiser is always overwritten
- `ObjectExtensions.cs:45` - the primitive/enum/decimal/DateTime disjunction is redundant,
  because a type with no writable properties falls through the complex-object branch unchanged

`IntExtensions.cs:13,18` also survive, as the deliberate empty sport-ID stubs that
`IntExtensionsTests` records.

The practical ceiling for this scope is therefore in the high 80s. Chasing a higher number
means deleting the dead code, not writing more tests.

## Not wired into CI

Deliberately manual or nightly, never a pull-request gate. CI already builds all 26 matrix images on
every push with no affected-files detection; adding a mutation run to that path would compound it for
little benefit. Run it when changing the code in scope, and after adding tests to it.

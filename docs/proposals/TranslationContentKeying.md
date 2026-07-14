# Design Proposal v2 — Content-Keyed Translations + Compact Store for `SportsbookDynamic`

**Status:** v2 DRAFT for review (supersedes v1). v1's "readers switch to hash lookup first,
dedupe later" was **wrong on performance** — see §0. No code beyond the already-merged,
flag-OFF read path until v2 is approved.
**Author:** generated with Claude Code, from repo evidence + production sampling (2026-06).

> Labels: **[verified]** confirmed in code/data · **[assumption]** reasoned, unconfirmed ·
> **[unknown]** must be verified before relying on it.

---

## 0. What happened with v1 (lesson learned)

v1 added a flag-gated read path that looked translations up by `SOURCE_HASH`, intending to
dedupe the table (writer) afterwards. Enabling it in staging made data retrieval **slow**.

Root cause **[verified by data]**: on the **un-deduplicated** table, one source text maps to
thousands of rows (markets are ~99.3% duplicated — 73K market ids → ~972 names). So
`WHERE SOURCE_HASH IN (...)` returns *every* duplicate row per text → huge per-request result
sets. Reading by content is only fast **after** the data is deduplicated.

**Conclusion:** dedupe must come **first**, not last. v2 reorders accordingly.

The v1 read path remains in the codebase but is **OFF** (`Custom:Translation:UseContentKey=false`,
forced false in all argocd envs). It will be replaced, not re-enabled as-is.

---

## 1. Problem & evidence (unchanged from v1)

`SportsbookDynamic` keys translations by entity `String_ID` (`MaId:123`), one row per
`(String_ID × language)`. Markets/selections churn constantly, so the table grows unbounded.

Production measurement **[verified]**:

| Metric | Value |
|---|---|
| Total rows | 26.7M |
| Size | ~18 GB (10.3 GB data + 7.6 GB indexes) |
| Distinct languages | 2 |
| Avg rows / String_ID | 1.99 (just the 2 languages) |
| Content duplication | ~84% overall; **MaId 99.3%**, SeId 78.8%, FiId 51.8% |

The duplication is concentrated in markets + selections (~99% of rows).

---

## 2. Current read/write (verified)

- **Write** — `BG.Translation.Service.BulkUpsert` inserts one row per entity id
  (`StringId="MaId:{id}"`, `SourceText=name`, EN). One per-language fill follows.
- **Read** — `LoadTranslations(...)` runs **once per request** (batched), building all keys, then
  `GetTranslationsAsync` does a **per-key MemCache loop + a SQL `IN` fallback**
  ([TranslationRepository.cs:55-101](../../Platform/Platform.Sportsbook.API/Repository/TranslationRepository.cs)).
  `GetTranslation(...)` per row is an in-memory dict lookup (cheap). OE2 mirrors this.
- **Enablers [verified]:** the source name (`MaNa`/`SeNa`/fixture names) is already in hand at read
  time; `SOURCE_HASH = CONVERT(varchar(64), HASHBYTES('SHA2_256', SOURCE_TEXT), 2)` and the C#
  helper reproduce it byte-for-byte (parity-tested); the writer already has a startup
  `PreLoadTranslationIntoCache()` pattern.

---

## 3. Target design (v2)

### 3a. Compact, content-keyed store
A new table keyed by content instead of entity id:

```
TranslationContent
  SOURCE_HASH           -- composite PK: (SOURCE_HASH, TARGET_LANGUAGE_ID, GLOSSARY_VERSION)
  TARGET_LANGUAGE_ID
  GLOSSARY_VERSION
  SOURCE_LANGUAGE_ID
  SOURCE_TEXT
  TARGET_TEXT
  PROCESSED
  HAS_BET_PLACEMENT     -- protects bet-referenced content from cleanup (carried from v1)
  CREATED_AT
  UPDATED_AT            -- touched whenever the writer re-sees the text = liveness signal
```
One row per distinct `(source text, language)`. Size = vocabulary, not entity count → collapses
26.7M → ~hundreds of thousands of rows; **growth bounded by vocabulary, not churn**.

**Cleanup is still required** (so `HAS_BET_PLACEMENT` stays in the schema): generic texts
("Money Line") are reused forever and never go stale, but the ~21% of *unique* selection names
(player/line-specific) become dead vocabulary once their fixtures end. The retention job
(`BG.Translation.Job`) therefore **evolves to the content model** rather than being retired:
- **Flag worker**: on bet placement, hash the `BetSelection` names (FixtureName/MarketName/
  SelectionName — already in the message) and set `HAS_BET_PLACEMENT = 1` on those content rows.
- **Cleanup worker**: delete content rows where `HAS_BET_PLACEMENT = 0` AND
  `UPDATED_AT < now - retention` (i.e. not re-seen for the window). The writer must **touch
  `UPDATED_AT` on re-sighting** (throttled, e.g. at most once/day per text) so live texts stay fresh.

### 3b. Writer = content-keyed dedupe (your point)
`BG.Translation.Service`: on each name, compute the hash; **upsert one row per `(hash, lang)`**
(skip if present). No more per-entity rows. The per-language translation fill keys by hash too.

### 3c. Readers = in-memory preload (your idea)
Because the store is now small, each reader **preloads the whole table into an in-memory
dictionary at startup** (`(hash,lang) → text`), refreshed periodically / on a CacheSync event.
Request path: hash the source name in hand → dict lookup. **No per-request SQL, no per-key cache
loop.** Falls back to the source (EN) text on a miss.

- **Preload is optional/config-gated.** The compact store *alone* already makes the lookup fast
  (≈1 row per hash). Preload is an added optimization that removes per-request SQL + cache-loop
  overhead. Enable it only after the memory check in §5 passes.

### 3d. Scope
Content-key markets + selections (where 99%/79% duplication is) and fixtures too (uniform model;
fixtures are low-volume so little downside). Sports/segments/leagues are tiny — included for
uniformity.

---

## 4. Phased migration (dedupe FIRST — the v1 fix)

Each phase is independently shippable and reversible.

1. **Backfill** the compact `TranslationContent` from existing `SportsbookDynamic`
   (`GROUP BY SOURCE_HASH, TARGET_LANGUAGE_ID`, keep highest `GLOSSARY_VERSION`). Read-only w.r.t.
   the old table; produces the small table. *(one-off job)*
2. **Writer dual-writes**: `BG.Translation.Service` writes the compact store (content-keyed) in
   addition to current behavior, so it stays fresh. *(old readers unaffected)*
3. **Readers switch** to the compact store (lookup by hash, optionally preloaded), flag-gated,
   validated by A/B (identical output) — now fast because the store is deduped.
4. **Retire** the old per-entity write path + (eventually) `SportsbookDynamic`. The retention job
   (`BG.Translation.Job`) is **re-pointed at `TranslationContent`** (flag by hashed bet names;
   clean stale unflagged content by `UPDATED_AT`) — it is **not** retired, because unique/dead
   vocabulary still needs cleanup. `HAS_BET_PLACEMENT` stays in the schema for this reason.

Ordering contrast with v1: **backfill/dedupe (1-2) happen before readers switch (3)** — the exact
inversion of the mistake in §0.

---

## 5. Gating measurement (run before building 3c)

Sizes the in-memory preload. Read-only.

```sql
-- distinct (text, lang) entries and their total bytes = preload footprint per pod
;WITH d AS (
    SELECT SOURCE_HASH, TARGET_LANGUAGE_ID, MAX(DATALENGTH(TARGET_TEXT)) AS bytes_
    FROM dbo.SportsbookDynamic WITH (NOLOCK)
    WHERE PROCESSED = 1 AND TARGET_TEXT IS NOT NULL
    GROUP BY SOURCE_HASH, TARGET_LANGUAGE_ID
)
SELECT COUNT(*) AS distinct_entries,
       SUM(CAST(bytes_ AS BIGINT)) / 1024.0 / 1024.0 AS target_text_mb
FROM d;
```
Decision: if `distinct_entries` × (entry + key overhead) is comfortably within the pod memory
budget (rule of thumb: < ~150 MB), enable preload; otherwise ship the compact store without
preload (still fast) and revisit.

---

## 6. Impact & risks

| Area | Change | Risk |
|---|---|---|
| Schema | new `TranslationContent` table + backfill | Medium (additive; old table stays during migration) |
| bet-genius writer | content-keyed upsert + dual-write | Medium |
| platform-final reader | lookup by hash from compact store, optional preload | Medium (central read path) |
| odds-engine-2 reader | same | Medium (sibling repo) |
| retention job / `HAS_BET_PLACEMENT` | re-pointed at `TranslationContent` (flag by hashed bet names; clean stale-by-`UPDATED_AT`) | Low |

- **[unknown] preload footprint** — gated by §5.
- **[assumption] `MaNa`/`SeNa` == stored `SOURCE_TEXT`** — still unvalidated (perf cut the A/B short); validate in phase 3 A/B.
- **Refresh/staleness** — define reload interval + CacheSync hook; new strings show EN until refresh.
- **R-2 cross-repo** — no contract test; phased + flag-gated rollout is the mitigation.
- **Dynamic/unique names** (~21% of selections, most fixtures) — get their own compact rows; correct, just less dedupe benefit there.

---

## 7. Recommendation

Adopt v2. Sequence: **(1) backfill → (2) writer dual-write → run §5 → (3) readers switch (+preload if sized OK) → (4) retire**. Nothing re-enables a content read path until the compact store exists and is populated.

Interim: the **retention job (`BG.Translation.Job`)** remains the low-risk immediate relief for table size and is independent of this redesign — worth shipping regardless.

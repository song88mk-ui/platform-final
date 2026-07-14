# /trace-translation

Trace how translated text is produced and served.

## Steps
1. **Store:** SQL `TranslationContext` — `SportsbookDynamic` (versioned, hashed, per-language; indexes
   incl. filtered `WHERE TARGET_TEXT IS NULL`) + `SportsbookStatic` (`LanguageCode`+`Name`→`Value`).
   `Platform.Shared/Data/Translation/`.
2. **Serve:** `Sportsbook.API` → `v1/sportsbook/translations` (`x-language-code`); `v1/translation/recovery`
   backfills missing. 21 `LanguageCode`s.
3. **Inline:** odds/fixture messages carry per-language arrays (`spNaTr/segNaTr/leNaTr/fiNaTr/maNaTr/seNaTr`)
   so live updates need no extra lookup.
4. **Consume (sibling):** platform-web caches in `TranslationState` (30s), `GetValue(key)`, static fallback.

## Reference
[docs/DomainModel.md](../../docs/DomainModel.md) §5 · [docs/Database.md](../../docs/Database.md)
(TranslationContext indexes) · [docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §8.

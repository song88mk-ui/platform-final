# Skill: Translation Investigation

Investigate multilingual content.

## Knowledge
- SQL `TranslationContext`: `SportsbookDynamic` (versioned, hashed, per-language; indexes incl.
  filtered `WHERE TARGET_TEXT IS NULL`) + `SportsbookStatic` (`LanguageCode`+`Name`→`Value`).
- Served by `Sportsbook.API` → `v1/sportsbook/translations`; `v1/translation/recovery` backfills.
- Also delivered **inline** on odds/fixture messages (`spNaTr/segNaTr/leNaTr/fiNaTr/maNaTr/seNaTr`).
- 21 `LanguageCode`s (heavily SE-Asian). Consumer caches in `TranslationState` (platform-web).

## Method
1. `/trace-translation`. 2. Missing text → check `IX_SBD_MISSING` (TARGET_TEXT IS NULL) +
   `GlossaryVersion`. 3. Wrong inline name → check the message's `*Tr` arrays vs the API dictionary.

## Reference
[docs/Database.md](../../docs/Database.md) (TranslationContext indexes), [docs/DomainModel.md](../../docs/DomainModel.md) §5.

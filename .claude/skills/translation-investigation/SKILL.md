---
name: translation-investigation
description: Investigate multilingual content (SQL SportsbookStatic labels, Mongo *NaTr inline names, the translations API). Use when text is missing, wrong or untranslated for a language.
---

# Skill: Translation Investigation

Investigate multilingual content.

## Knowledge
- SQL `TranslationContext` = `SportsbookStatic` only (`LanguageCode`+`Name`→`Value`); dynamic names
  come from the Mongo `*NaTr` lists (sourced from odds-engine-2 `TranslationCache2`).
- Served by `Sportsbook.API` → `v1/sportsbook/translations`; `v1/translation/recovery` backfills.
- Also delivered **inline** on odds/fixture messages (`spNaTr/segNaTr/leNaTr/fiNaTr/maNaTr/seNaTr`).
- 22 `LanguageCode`s (heavily SE-Asian). Consumer caches in `TranslationState` (platform-web).

## Method
1. `/trace-translation`. 2. Missing text → check the Mongo `*NaTr` list for the language, then
   odds-engine-2 `TranslationCache2` / the translation pipeline. 3. Wrong inline name → check the message's `*Tr` arrays vs the API dictionary.

## Reference
[docs/Database.md](../../../docs/Database.md) (TranslationContext), [docs/DomainModel.md](../../../docs/DomainModel.md) §5.

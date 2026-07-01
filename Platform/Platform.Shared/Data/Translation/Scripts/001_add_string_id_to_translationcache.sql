/*
    Translation DB — add STRING_ID to TranslationCache, defaulted to SOURCE_HASH.

    STRING_ID is the SportsbookGlossary.String_ID key that produced this row (e.g.
    'MaTe:1234', 'SeTe:1234:56', 'Te:789', 'SpId:1'). Platform.Sportsbook.API resolves
    translations by STRING_ID directly — built from the same ids as SportsbookGlossary —
    instead of hashing source text.

    NOT NULL is intentionally NOT enforced here: TranslationCache is still written by the
    external translation script, which does not yet populate STRING_ID. A hard NOT NULL /
    DEFAULT constraint can't derive its value from another column (SOURCE_HASH) in the same
    row, and would start rejecting that service's inserts if it omits the column or passes
    NULL explicitly. Instead, a trigger keeps STRING_ID defaulted to SOURCE_HASH for any row
    left NULL/empty by the writer — no coordination with the external script required. Once
    that script is updated to populate real glossary keys, this remains a safe no-op for the
    rows it now supplies.

    Apply manually against the Translation database. Idempotent; safe to re-run.
*/

-- 1. Add nullable (existing rows, and rows from writers that don't yet supply it, need to
--    tolerate NULL — the trigger below fills it in immediately after every insert/update).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.TranslationCache')
      AND name = 'STRING_ID')
BEGIN
    ALTER TABLE dbo.TranslationCache
        ADD STRING_ID NVARCHAR(200) NULL;
END
GO

-- 2. One-time backfill for rows that already exist.
UPDATE dbo.TranslationCache
SET STRING_ID = SOURCE_HASH
WHERE STRING_ID IS NULL OR STRING_ID = N'';
GO

-- 3. Ongoing default for the external script's inserts/updates: whenever STRING_ID is left
--    NULL/empty, set it to that row's own SOURCE_HASH. Runs in the same transaction as the
--    writer's statement, so every committed row always has a STRING_ID.
IF OBJECT_ID('dbo.trg_TranslationCache_DefaultStringId', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_TranslationCache_DefaultStringId;
GO

CREATE TRIGGER dbo.trg_TranslationCache_DefaultStringId
ON dbo.TranslationCache
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE tc
        SET tc.STRING_ID = tc.SOURCE_HASH
    FROM dbo.TranslationCache tc
    INNER JOIN inserted i
        ON tc.SOURCE_HASH = i.SOURCE_HASH
       AND tc.SOURCE_LANGUAGE = i.SOURCE_LANGUAGE
       AND tc.TARGET_LANGUAGE = i.TARGET_LANGUAGE
    WHERE i.STRING_ID IS NULL OR i.STRING_ID = N'';
END
GO

-- 4. Supporting index for the STRING_ID-based read path (replaces the old SOURCE_HASH lookup).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TranslationCache_StringId'
      AND object_id = OBJECT_ID('dbo.TranslationCache'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TranslationCache_StringId
        ON dbo.TranslationCache (STRING_ID, TARGET_LANGUAGE)
        INCLUDE (SOURCE_LANGUAGE, TARGET_TEXT);
END
GO

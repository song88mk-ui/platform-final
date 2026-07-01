/*
    Betting DB — add IsHandicap to BetSelection, defaulted to 0 (false).

    Captures the live Mongo Market.IsHa flag at bet placement time, so bet-history/manual-bet-details
    translation (GetBetFixtureTranslation) can recompose a translated selection template name with its
    line ("Over/Under 2.5") the same way live-catalogue lookups do (ComposeSelectionName), instead of
    resolving a fully pre-composed name.

    Bets placed before this column existed have no way to know whether their market was a handicap
    market, so they default to 0/false: their selection translation falls back to the bare translated
    template name without a recomposed line suffix.

    Apply manually against the Betting database. Idempotent; safe to re-run.
*/

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.BetSelection')
      AND name = 'IsHandicap')
BEGIN
    ALTER TABLE dbo.BetSelection
        ADD IsHandicap BIT NOT NULL CONSTRAINT DF_BetSelection_IsHandicap DEFAULT (0);
END
GO

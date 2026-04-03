CREATE PROCEDURE InserOrUpdateClientMarketSortOrder
(
    @ClientId UNIQUEIDENTIFIER,
    @SportId INT,
    @FixtureStatusId INT,
    @Name NVARCHAR(255),
    @MarketTemplates NVARCHAR(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Check if record exists
        IF EXISTS (
            SELECT 1
            FROM ClientMarketTemplateGroup
            WHERE ClientId = @ClientId
              AND SportId = @SportId
              AND FixtureStatusId = @FixtureStatusId
              AND LOWER(Name) = LOWER(@Name)
        )
        BEGIN
            -- UPDATE existing
            UPDATE ClientMarketTemplateGroup
            SET 
                MarketTemplates = @MarketTemplates,
                -- Optional: maintain sort order if needed
                SortOrder = SortOrder,
                IsActive = 1
            WHERE ClientId = @ClientId
              AND SportId = @SportId
              AND FixtureStatusId = @FixtureStatusId
              AND LOWER(Name) = LOWER(@Name);
        END
        ELSE
        BEGIN
            -- INSERT new
            INSERT INTO ClientMarketTemplateGroup
            (
                ClientId,
                SportId,
                FixtureStatusId,
                Name,
                SortOrder,
                IsActive,
                MarketTemplates
            )
            VALUES
            (
                @ClientId,
                @SportId,
                @FixtureStatusId,
                @Name,
                0,              -- default sort order (can adjust later)
                1,              -- active by default
                @MarketTemplates
            );
        END

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
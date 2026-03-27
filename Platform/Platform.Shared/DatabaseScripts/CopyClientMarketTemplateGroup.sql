CREATE PROCEDURE CopyClientMarketTemplateGroup
(
    @SourceClientId UNIQUEIDENTIFIER,
    @SourceSportId INT,
    @SourceFixtureStatusId INT,
    @TargetClientId UNIQUEIDENTIFIER,
    @TargetSportId INT,
    @TargetFixtureStatusId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Clean target first
        DELETE FROM ClientMarketTemplateGroup
        WHERE ClientId = @TargetClientId
          AND SportId = @TargetSportId
          AND FixtureStatusId = @TargetFixtureStatusId;

        -- Insert copied data
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
        SELECT
            @TargetClientId,
            @TargetSportId,
            @TargetFixtureStatusId,
            Name,
            SortOrder,
            IsActive,
            MarketTemplates
        FROM ClientMarketTemplateGroup
        WHERE ClientId = @SourceClientId
          AND SportId = @SourceSportId
          AND FixtureStatusId = @SourceFixtureStatusId;

    END TRY
    BEGIN CATCH
        -- Optional: return error info
        DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
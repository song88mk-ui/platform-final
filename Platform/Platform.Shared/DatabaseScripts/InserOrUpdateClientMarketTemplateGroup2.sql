CREATE PROCEDURE InserOrUpdateClientMarketTemplateGroup2
(
    @ClientId UNIQUEIDENTIFIER,
    @SportId INT,
    @FixtureStatusId INT,
    @MarketTemplateGroupId INT,
    @Name NVARCHAR(100),
    @SortOrder INT,
    @IsActive BIT,
    @MarketTemplateIds NVARCHAR(MAX)
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingId INT;

    -- 1. Priority: Use MarketTemplateGroupId if provided
    IF (@MarketTemplateGroupId IS NOT NULL AND @MarketTemplateGroupId > 0)
    BEGIN
        SELECT @ExistingId = Id
        FROM ClientMarketTemplateGroup
        WHERE Id = @MarketTemplateGroupId
          AND ClientId = @ClientId
          AND SportId = @SportId;
    END
    ELSE
    BEGIN
        -- 2. Fallback: Match by composite keys
        SELECT @ExistingId = Id
        FROM ClientMarketTemplateGroup
        WHERE ClientId = @ClientId
          AND SportId = @SportId
          AND FixtureStatusId = @FixtureStatusId
          AND LOWER(Name) = LOWER(@Name);
    END

    -- 3. UPDATE if exists
    IF (@ExistingId IS NOT NULL)
    BEGIN
        UPDATE ClientMarketTemplateGroup
        SET 
            Name = @Name,
            SortOrder = @SortOrder,
            IsActive = @IsActive,
            MarketTemplates = @MarketTemplateIds,
            FixtureStatusId = @FixtureStatusId
        WHERE Id = @ExistingId;
    END
    ELSE
    BEGIN
        -- 4. INSERT if not exists
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
            @SortOrder,
            @IsActive,
            @MarketTemplateIds
        );
    END
END
GO
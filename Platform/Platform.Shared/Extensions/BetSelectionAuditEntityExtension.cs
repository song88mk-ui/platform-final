using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting.Entities;

namespace Platform.Shared.Extensions
{
    public static class BetSelectionAuditEntityExtension
    {
        public static EntityEntry<BetSelectionAudit> AddAudit(this DbSet<BetSelectionAudit> audit, BetSelection entity)
        {
            return audit.Add(new BetSelectionAudit
            {
                BetId = entity.BetId,
                BetSelectionId = entity.BetSelectionId,
                UserId = entity.UserId,
                SportId = entity.SportId,
                SportName = entity.SportName,
                SegmentId = entity.SegmentId,
                SegmentName = entity.SegmentName,
                LeagueId = entity.LeagueId,
                LeagueName = entity.LeagueName,
                FixtureId = entity.FixtureId,
                FixtureName = entity.FixtureName,
                FixtureTypeId = entity.FixtureTypeId,
                FixtureTypeName = entity.FixtureTypeName,
                MarketTemplateId = entity.MarketTemplateId,
                MarketId = entity.MarketId,
                MarketName = entity.MarketName,
                SelectionTemplateId = entity.SelectionTemplateId,
                SelectionId = entity.SelectionId,
                SelectionName = entity.SelectionName,
                FixtureStatusId = entity.FixtureStatusId,
                FixtureStatusName = entity.FixtureStatusName,
                Line = entity.Line,
                Odds = entity.Odds,
                OddsRef = entity.OddsRef,
                BetSelectionStatusId = entity.BetSelectionStatusId,
                BetSelectionStatusName = entity.BetSelectionStatusName,
                IsSettled = entity.IsSettled,
                IsResettled = entity.IsResettled,
                FixtureSpecifier = entity.FixtureSpecifier,
                SelectionSpecifier = entity.SelectionSpecifier,
                FixtureStartDate = entity.FixtureStartDate,
                BetScore = entity.BetScore,
                SettlementScore = entity.SettlementScore,
                Probability = entity.Probability,
                WonProbability = entity.WonProbability,
                LostProbability = entity.LostProbability,
                VoidProbability = entity.VoidProbability,
                HalfWonProbability = entity.HalfWonProbability,
                HalfLostProbability = entity.HalfLostProbability,
                CorrelationId = entity.CorrelationId,
                DeadHeatFactor = entity.DeadHeatFactor,
                IsBinary = entity.IsBinary
            });
        }
    }
}

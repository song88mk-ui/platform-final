using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Shared.Utility
{
    public class ReportAdapter
    {
        public static BetModelRequestDTO ConvertBetAndSelection(Bet bet, List<BetSelection> selections)
        {
            var sel = selections.FirstOrDefault();
            return new BetModelRequestDTO
            {
                BetId = bet.BetId,
                UserId = bet.UserId,
                Username = "", // TODO: get value
                ApplicationId = 0, // TODO: get value
                ClientId = 0, // TODO: get value
                ClientName = "", // TODO: get value
                CompanyShare = bet.CompanyShare,
                ClientShare = (decimal)1.0 - bet.CompanyShare,
                GGR = 0, // TODO: get value
                Stake = bet.Stake,
                TrueStake = bet.TrueStake,
                Odds = bet.Odds,
                OddsTypeId = bet.OddsTypeId,
                OddsTypeName = bet.OddsTypeName,
                Payout = bet.Payout,
                WinLoss = bet.WinLoss,
                StatusId = bet.BetStatusId,
                StatusName = bet.BetStatusName,
                Reason = bet.Reason,
                CurrencyCode = bet.CurrencyCode,
                ExchangeRate = bet.ExchangeRate,
                IPAddress = bet.IPAddress,
                Count = selections.Count,
                SportId = sel.SportId,
                SportName = sel.SportName,
                SegmentId = sel.SegmentId,
                SegmentName = sel.SegmentName,
                LeagueId = sel.LeagueId,
                LeagueName = sel.LeagueName,
                FixtureId = sel.FixtureId,
                FixtureName = sel.FixtureName,
                FixtureStatusId = sel.FixtureStatusId,
                FixtureStatusName = sel.FixtureStatusName,
                FixtureStartDate = sel.FixtureStartDate,
                Score = "(Home) " + sel.BetHomeScore + " " + "(Away) " + sel.BetAwayScore,
                MarketTemplateGroupId = 0, // TODO: get value
                MarketTemplateGroupName = "", // TODO: get value
                MarketTemplateId = 0, // TODO: get value
                MarketId = sel.MarketId,
                MarketName = sel.MarketName,
                Line = 0, // TODO: get value
                SelectionTemplateId = 0,// TODO: get value
                SelectionId = sel.SelectionId,
                SelectionName = sel.SelectionName,
                TransactionDate = bet.BetDate,
                TransactionDateOffset = bet.BetDateOffset,
                SettlementDate = bet.SettlementDate,
                SettlementDateOffset = bet.SettlementDateOffset,
                MarketTypeId = 0, // TODO: get value
                BetSelections = selections.Select(b => new ReportBetSelectionDTO()
                {
                    BetId = bet.BetId,
                    BetSelectionId = b.BetSelectionId,
                    ClientId = 0,// TODO: get value
                    UserId = "",// TODO: get value
                    SportId = b.SportId,
                    SportName = b.SportName,
                    SegmentId = b.SegmentId,
                    SegmentName = b.SegmentName,
                    LeagueId = b.LeagueId,
                    LeagueName = b.LeagueName,
                    FixtureId = b.FixtureId,
                    FixtureName = b.FixtureName,
                    MarketId = sel.MarketId,
                    MarketName = sel.MarketName,
                    SelectionId = sel.SelectionId,
                    SelectionName = sel.SelectionName,
                    FixtureStatusId = sel.FixtureStatusId,
                    FixtureStatusName = sel.FixtureStatusName,
                    Line = 0, // TODO: get value
                    Odds = bet.Odds,
                    StatusId = bet.BetStatusId,
                    StatusName = bet.BetStatusName,
                    IsSettled = b.IsSettled,
                    IsResettled = b.IsResettled,
                    FixtureSpecifier = b.FixtureSpecifier,
                    SelectionSpecifier = b.SelectionSpecifier,
                    FixtureStartDate = b.FixtureStartDate,
                    MarketTemplateGroupId = 0,// TODO: get value
                    MarketTemplateId = 0,// TODO: get value
                    SelectionTemplateId = 0,// TODO: get value
                    Score = "(Home) " + b.BetHomeScore + " " + "(Away) " + b.BetAwayScore,
                    BetScore = "", // TODO: get value
                    FinalScore = "", // TODO: get value
                    HalftimeScore = "", // TODO: get value
                })
            };
        }
        public static ReportBetSelectionDTO ConvertSelection(Bet bet, BetSelection selection)
        {
            return new ReportBetSelectionDTO()
            {
                BetId = bet.BetId,
                BetSelectionId = selection.BetSelectionId,
                ClientId = 0,// TODO: get value
                UserId = "",// TODO: get value
                SportId = selection.SportId,
                SportName = selection.SportName,
                SegmentId = selection.SegmentId,
                SegmentName = selection.SegmentName,
                LeagueId = selection.LeagueId,
                LeagueName = selection.LeagueName,
                FixtureId = selection.FixtureId,
                FixtureName = selection.FixtureName,
                MarketId = selection.MarketId,
                MarketName = selection.MarketName,
                SelectionId = selection.SelectionId,
                SelectionName = selection.SelectionName,
                FixtureStatusId = selection.FixtureStatusId,
                FixtureStatusName = selection.FixtureStatusName,
                Line = 0, // TODO: get value
                Odds = bet.Odds,
                StatusId = bet.BetStatusId,
                StatusName = bet.BetStatusName,
                IsSettled = selection.IsSettled,
                IsResettled = selection.IsResettled,
                FixtureSpecifier = selection.FixtureSpecifier,
                SelectionSpecifier = selection.SelectionSpecifier,
                FixtureStartDate = selection.FixtureStartDate,
                MarketTemplateGroupId = 0,// TODO: get value
                MarketTemplateId = 0,// TODO: get value
                SelectionTemplateId = 0,// TODO: get value
                Score = "(Home) " + selection.BetHomeScore + " " + "(Away) " + selection.BetAwayScore,
                BetScore = "", // TODO: get value
                FinalScore = "", // TODO: get value
                HalftimeScore = "", // TODO: get value
            };
        }
    }
}

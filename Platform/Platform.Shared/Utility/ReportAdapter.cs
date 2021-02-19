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
            BetModelRequestDTO payload = new BetModelRequestDTO
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
                SportId = selections.FirstOrDefault().SportId,
                SportName = selections.FirstOrDefault().SportName,
                SegmentId = selections.FirstOrDefault().SegmentId,
                SegmentName = selections.FirstOrDefault().SegmentName,
                LeagueId = selections.FirstOrDefault().LeagueId,
                LeagueName = selections.FirstOrDefault().LeagueName,
                FixtureId = selections.FirstOrDefault().FixtureId,
                FixtureName = selections.FirstOrDefault().FixtureName,
                FixtureStatusId = selections.FirstOrDefault().FixtureStatusId,
                FixtureStatusName = selections.FirstOrDefault().FixtureStatusName,
                FixtureStartDate = selections.FirstOrDefault().FixtureStartDate,
                Score = "(Home) " + selections.FirstOrDefault().BetHomeScore + " " + "(Away) " + selections.FirstOrDefault().BetAwayScore,
                MarketTemplateGroupId = 0, // TODO: get value
                MarketTemplateGroupName = "", // TODO: get value
                MarketTemplateId = 0, // TODO: get value
                MarketId = selections.FirstOrDefault().MarketId,
                MarketName = selections.FirstOrDefault().MarketName,
                Line = 0, // TODO: get value
                SelectionTemplateId = 0,// TODO: get value
                SelectionId = selections.FirstOrDefault().SelectionId,
                SelectionName = selections.FirstOrDefault().SelectionName,
                TransactionDate = bet.BetDate,
                TransactionDateOffset = bet.BetDateOffset,
                SettlementDate = bet.SettlementDate,
                SettlementDateOffset = bet.SettlementDateOffset,
                MarketTypeId = 0 // TODO: get value
            };
            return payload;
        }
        public static ReportBetSelectionDTO ConvertSelection(BetSelection selection)
        {
            ReportBetSelectionDTO payload = new ReportBetSelectionDTO()
            {
            };
            return payload;
        }
    }
}

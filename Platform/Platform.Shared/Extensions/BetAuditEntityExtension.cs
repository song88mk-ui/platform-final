using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting.Entities;

namespace Platform.Shared.Extensions
{
    public static class BetAuditEntityExtension
    {
        public static EntityEntry<BetAudit> AddAudit(this DbSet<BetAudit> audit, Bet enity)
        {
            return audit.Add(new BetAudit
            {
                BetId = enity.BetId,
                UserId = enity.UserId,
                Stake = enity.Stake,
                TrueStake = enity.TrueStake,
                Odds = enity.Odds,
                OddsTypeId = enity.OddsTypeId,
                OddsTypeName = enity.OddsTypeName,
                Payout = enity.Payout,
                WinLoss = enity.WinLoss,
                BetStatusId = enity.BetStatusId,
                BetStatusName = enity.BetStatusName,
                Reason = enity.Reason,
                CurrencyCode = enity.CurrencyCode,
                LanguageCode = enity.LanguageCode,
                ExchangeRate = enity.ExchangeRate,
                IPAddress = enity.IPAddress,
                Channel = enity.Channel,
                ClientId = enity.ClientId,
                ClientIdRef = enity.ClientIdRef,
                ClientCode = enity.ClientCode,
                ClientName = enity.ClientName,
                LimitId = enity.LimitId,
                Endpoint = enity.Endpoint,
                OperatorToken = enity.OperatorToken,
                CompanyShare = enity.CompanyShare,
                IsResettled = enity.IsResettled,
                IsCashout = enity.IsCashout,
                BetDate = enity.BetDate,
                BetDateOffset = enity.BetDateOffset,
                SettlementDate = enity.SettlementDate,
                SettlementDateOffset = enity.SettlementDateOffset,
                StakeTax = enity.StakeTax,
                WinningsTax = enity.WinningsTax,
                TaxedStake = enity.TaxedStake,
                TaxedWinnings = enity.TaxedWinnings,
                ParlayBonus = enity.ParlayBonus,
                ParlayBonusWinnings = enity.ParlayBonusWinnings,
                StakeReal = enity.StakeReal,
                StakeBonus = enity.StakeBonus,
                StakeRealPayout = enity.StakeRealPayout,
                StakeBonusPayout = enity.StakeBonusPayout,
                IsFreeBet = enity.IsFreeBet,
                BetTypeId = enity.BetTypeId,
                BetTypeName = enity.BetTypeName,
                OddsRef = enity.OddsRef,
                IsAgent = enity.IsAgent,
                BetIdRef = enity.BetIdRef
            });
        }
    }
}

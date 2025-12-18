using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;
using System.Linq;
using System.Transactions;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;

namespace Platform.Shared.Services
{
    public sealed class ParlayBonusService : IParlayBonusService
    {
        private readonly IConfiguration _configuration;

        public ParlayBonusService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (bool HasBonus, ParlayBonusAudit Config) GetParlayBonus(
            Guid clientId,
            string currencyCode,
            DateTime betDate,
            IReadOnlyList<BetSelection> selections,
            decimal payout)
        {
            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            var options = new DbContextOptionsBuilder<BettingContext>()
                .UseSqlServer(_configuration["ConnectionStrings:Betting"])
                .Options;

            using var context = new BettingContext(options);

            var minimumOdds = context.ParlayBonusAudit
                .Where(c => c.ClientId == clientId
                         && c.CurrencyCode == currencyCode
                         && c.TimeStamp < betDate)
                .OrderByDescending(o => o.TimeStamp)
                .FirstOrDefault();

            if (minimumOdds == null)
                return (false, new ParlayBonusAudit());

            var wonSelectionCount = selections.Count(s =>
                s.BetSelectionStatusId == (int)BetStatus.Won &&
                s.Odds >= minimumOdds.ParlayBonusMinOdds);

            if (wonSelectionCount < 3)
                return (false, new ParlayBonusAudit());

            var result = context.ParlayBonusAudit
                .Where(c => c.ClientId == clientId
                         && c.CurrencyCode == currencyCode
                         && c.TimeStamp < betDate
                         && c.SelectionCount == wonSelectionCount)
                .OrderByDescending(o => o.TimeStamp)
                .FirstOrDefault();

            return result == null
                ? (false, new ParlayBonusAudit())
                : (true, result);
        }
    }
}

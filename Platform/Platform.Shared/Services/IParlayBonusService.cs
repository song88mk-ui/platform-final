using Platform.Shared.Data.Betting.Entities;
using System.Collections.Generic;
using System;

namespace Platform.Shared.Services
{
    public interface IParlayBonusService
    {
        (bool HasBonus, ParlayBonusAudit Config) GetParlayBonus(
            Guid clientId,
            string currencyCode,
            DateTime betDate,
            IReadOnlyList<BetSelection> selections,
            decimal payout);
    }
}

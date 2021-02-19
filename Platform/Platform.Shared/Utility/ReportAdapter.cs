using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Shared.Utility
{
    public class ReportAdapter
    {
        public static BetModelRequestDTO ConvertBetAndSelection(Bet bet, BetSelection selection)
        {
            BetModelRequestDTO payload = new BetModelRequestDTO()
            {
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

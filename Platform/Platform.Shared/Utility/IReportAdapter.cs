using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Shared.Utility
{
    public interface IReportAdapter
    {
        BetModelRequestDTO ConvertToBetModel(Bet bet, List<BetSelection> selections);
        ReportBetSelectionDTO ConvertToSelection(Bet bet, BetSelection selection);
    }
}

using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IMTSProbabilityRepository
    {
        Task<BaseResponseDTO<CashOutResponse>> GetCashOut(Bet bet, List<BetSelection> betSelections);
    }
}

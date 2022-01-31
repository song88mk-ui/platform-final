using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IProbabilityRepository
    {
        Task<BaseResponseDTO<List<CashOutDetailsReponse>>> GetCashOut(List<Bet> bets, List<BetSelection> betSelections, List<Guid> boostedList);
    }
}

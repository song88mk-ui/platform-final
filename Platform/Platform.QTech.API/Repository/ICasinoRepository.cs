using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using System.Threading.Tasks;
using System;

namespace Platform.QTech.API.Repository
{
    public interface ICasinoRepository
    {
        Task<Tuple<SessionCode, GameSessionResponse>> GetSession(GameSessionRequest request);
        Task<Tuple<BalanceCode, BalanceResponse>> GetBalance(BalanceRequest request);
        Task<Tuple<WithdrawCode, WithdrawResponse>> Withdraw(WithdrawRequest request);
        Task<Tuple<DepositCode, DepositResponse>> Deposit(DepositRequest request);
        Task<Tuple<RollbackCode, RollbackResponse>> Rollback(RollbackRequest request);
        Task<BonusStatusCode> ProcessBonusStatus(BonusStatusRequest request);
        Task<Tuple<BonusRewardCode, BonusRewardResponse>> ProcessBonusReward(BonusRewardRequest request);
        Task<Tuple<GameLobbyCode, GameLobbyResponse>> GetLobbyURL(GameLobbyRequestDTO request);
    }
}

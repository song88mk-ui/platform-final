using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using System;
using System.Threading.Tasks;

namespace Platform.QTech.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        public async Task<Tuple<SessionCode, GameSessionResponse>> GetSession(GameSessionRequest request)
        {
            return new Tuple<SessionCode, GameSessionResponse>(SessionCode.OK, new GameSessionResponse());
        }
        public async Task<Tuple<BalanceCode, BalanceResponse>> GetBalance(BalanceRequest request)
        {
            return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse());
        }
        public async Task<Tuple<WithdrawCode, WithdrawResponse>> Withdraw(WithdrawRequest request)
        {
            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse());
        }
        public async Task<Tuple<DepositCode, DepositResponse>> Deposit(DepositRequest request)
        {
            return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse());
        }
        public async Task<Tuple<RollbackCode, RollbackResponse>> Rollback(RollbackRequest request)
        {
            return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse());
        }
        public async Task<BonusStatusCode> ProcessBonusStatus(BonusStatusRequest request)
        {
            return BonusStatusCode.OK;
        }
        public async Task<Tuple<BonusRewardCode, BonusRewardResponse>> ProcessBonusReward(BonusRewardRequest request)
        {
            return new Tuple<BonusRewardCode, BonusRewardResponse>(BonusRewardCode.OK, new BonusRewardResponse());
        }
        public async Task<Tuple<AuthenticationCode, AuthenticationResponse>> AuthenticateToQT(AuthenticationRequest request)
        {
            return new Tuple<AuthenticationCode, AuthenticationResponse>(AuthenticationCode.OK, new AuthenticationResponse());
        }
    }
}

using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Platform.QTech.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IMemCache _memCache;

        public CasinoRepository(IProviderRepository providerRepository, IMemCache memCache)
        {
            _providerRepository = providerRepository;
            _memCache = memCache;
        }
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
        public async Task<Tuple<GameLobbyCode, GameLobbyResponseDTO>> GetLobbyURL(GameLobbyRequestDTO request)
        {
            var key = "qttoken";
            var token = _memCache.GetCache<AuthenticationResponse>(key);

            if (token == null)
            {
                var initialResult = await _providerRepository.GetToken();

                if (initialResult.Item1 == AuthenticationCode.OK)
                {
                    token = initialResult.Item2;
                    _memCache.AddOrUpdateCache(key, token, 5);
                }
                else
                    return new Tuple<GameLobbyCode, GameLobbyResponseDTO>(GameLobbyCode.PROVIDER_ERROR, new GameLobbyResponseDTO());
            }

            var payload = new GameLobbyRequest
            {
                Token = token.Access_token,
                PlayerId = request.UserId,
                Currency = request.CurrencyCode,
                Country = request.CountryCode,
                Lang = $"{request.LanguageCode}_{request.CountryCode}",
                Mode = request.IsDemo ? "DEMO" : "REAL_ONLY",
                Device = request.IsMobile ? "mobile" : "desktop",
                WalletSessionId = request.OperatorToken,
                Balance = request.ShowBalance,
                GameHistory = request.ShowBetHistory
            };

            var result = await _providerRepository.GetGameLobbyURL(payload);

            if (result.Item1 == GameLobbyCode.INVALID_TOKEN)
            {
                _memCache.DeleteCache("qttoken");

                 return await GetLobbyURL(request);
            }
            else
            {
                return new Tuple<GameLobbyCode, GameLobbyResponseDTO>(result.Item1, new GameLobbyResponseDTO { Url = result.Item2.Url });
            }
        }
    }
}

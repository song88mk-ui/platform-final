using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.QTech.API.Repository
{
    public class CasinoRepository : ICasinoRepository
    {
        private readonly IProviderRepository _providerRepository;
        private readonly IOperatorRepository _operatorRepository;
        private readonly IMemCache _memCache;
        private readonly PlatformContext _platformContext;

        public CasinoRepository(IProviderRepository providerRepository, IMemCache memCache, PlatformContext platformContext, IOperatorRepository operatorRepository)
        {
            _providerRepository = providerRepository;
            _memCache = memCache;
            _platformContext = platformContext;
            _operatorRepository = operatorRepository;
        }
        public async Task<Tuple<SessionCode, GameSessionResponse>> GetSession(GameSessionRequest request)
        {
            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.ACCOUNT_BLOCKED, new GameSessionResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<SessionCode, GameSessionResponse>(SessionCode.INVALID_TOKEN, new GameSessionResponse());

            return new Tuple<SessionCode, GameSessionResponse>(SessionCode.OK, new GameSessionResponse 
            {
                Balance = authenticationResponse.Result.Balance,
                Currency = authenticationResponse.Result.CurrencyCode
            });
        }
        public async Task<Tuple<BalanceCode, BalanceResponse>> GetBalance(BalanceRequest request)
        {
            var split = request.WalletSession.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.REQUEST_DECLINED, new BalanceResponse());

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != StatusCode.Success)
                return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.LOGIN_FAILED, new BalanceResponse());

            return new Tuple<BalanceCode, BalanceResponse>(BalanceCode.OK, new BalanceResponse
            {
                Balance = authenticationResponse.Result.Balance,
                Currency = authenticationResponse.Result.CurrencyCode
            });
        }
        public async Task<Tuple<WithdrawCode, WithdrawResponse>> Withdraw(WithdrawRequest request)
        {
            //connect to platform database to validate clientid
            //call platform.operator.api to debit
            //once successful/failed, store debit transaction in CasinoBetting database

            return new Tuple<WithdrawCode, WithdrawResponse>(WithdrawCode.OK, new WithdrawResponse());
        }
        public async Task<Tuple<DepositCode, DepositResponse>> Deposit(DepositRequest request)
        {
            //connect to platform database to validate clientid
            //call platform.operator.api to debit
            //once successful/failed, store credit transaction in CasinoBetting database

            return new Tuple<DepositCode, DepositResponse>(DepositCode.OK, new DepositResponse());
        }
        public async Task<Tuple<RollbackCode, RollbackResponse>> Rollback(RollbackRequest request)
        {
            //connect to platform database to validate clientid
            //call platform.operator.api to debit
            //once successful/failed, store credit transaction in CasinoBetting database

            return new Tuple<RollbackCode, RollbackResponse>(RollbackCode.OK, new RollbackResponse());
        }
        public async Task<BonusStatusCode> ProcessBonusStatus(BonusStatusRequest request)
        {
            return BonusStatusCode.OK;
        }
        public async Task<Tuple<BonusRewardCode, BonusRewardResponse>> ProcessBonusReward(BonusRewardRequest request)
        {
            //connect to platform database to validate clientid
            //call platform.operator.api to debit
            //once successful/failed, store credit transaction in CasinoBetting database

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
                GameHistory = request.ShowBetHistory,
                GameTypes = request.GameTypes
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



        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var key = $"cc:{clientId.ToUpper()}";

            var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

            if (cache != null)
                return cache;

            var result = await _platformContext.Client.FindAsync(Guid.Parse(clientId));

            if (result == null || result.IsActive == false)
            {
                return new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }
            else
            {
                var finalResult = new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = new ClientResponse
                    {
                        ClientId = result.ClientId,
                        ClientName = result.ClientName,
                        ClientCode = result.ClientCode,
                        LimitId = result.LimitId,
                        IsActive = result.IsActive,
                        Endpoint = result.Endpoint,
                        CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                        ClientIdRef = result.ClientIdRef,
                        IsAgent = result.IsAgent
                    }
                };

                _memCache.AddOrUpdateCache(key, finalResult, 24);

                return finalResult;
            }
        }
    }

    public class ClientResponse
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public int ClientIdRef { get; set; }
        public bool IsAgent { get; set; }
    }
}

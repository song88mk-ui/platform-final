using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.API.Telemetry;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;

namespace Platform.API.Test.Repository
{
    // Covers the BO-106 guardrail in PlatformRepository.CashoutBet (Platform.API/Repository/
    // PlatformRepository.cs, ~line 3305): BO's Full/Partial Cashout toggles must be enforced here,
    // not just by hiding the button in platform-web. Fixed by commit 7c4718d3 ("Fix webprops
    // lookup key and stop defaulting to staging webprops"), which switched the lookup key from
    // ClientCode to the client GUID after BO-96 QA found cashout still succeeding on stg with
    // Full Cashout disabled. These tests pin both halves of that fix: the key used, and the
    // allow/deny decision for each flag combination.
    public class CashoutBetWebPropsGuardrailTests
    {
        private sealed class SpyWebPropsRepository : IWebPropsRepository
        {
            public string? LastRequestedKey { get; private set; }
            private readonly BaseResponseDTO<WebPropsConfigResponseDTO> _response;

            public SpyWebPropsRepository(BaseResponseDTO<WebPropsConfigResponseDTO> response)
            {
                _response = response;
            }

            public Task<BaseResponseDTO<WebPropsConfigResponseDTO>> GetClientFeatureFlags(string clientId)
            {
                LastRequestedKey = clientId;
                return Task.FromResult(_response);
            }
        }

        private sealed class AlwaysAuthenticatesOperatorRepository : IOperatorRepository
        {
            public Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string clientId, string operatorToken, string endpoint, bool isAgent, OGClientRequest request) =>
                Task.FromResult(new BaseResponseDTO<AuthenticationResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new AuthenticationResponseDTO { UserId = Guid.NewGuid().ToString() }
                });

            public Task<BaseResponseDTO<LoginResponseDTO>> Login(string userName, string password, string endpoint, Dictionary<string, string>? headers = null) =>
                throw new NotImplementedException();

            public Task<BaseResponseDTO<object>> Logout(string operatorToken, string endpoint) =>
                throw new NotImplementedException();

            public Task<BaseResponseDTO<OperatorBetResponseDTO>> ResendCancellation(string endpoint, string info) =>
                throw new NotImplementedException();
        }

        // Only CashOutBet is reachable once the guardrail passes; every other member of this
        // large integration-provider interface is untouched by PlatformRepository.CashoutBet.
        private sealed class RecordingSuccessBettingRepository : IBettingRepository
        {
            public bool CashOutBetCalled { get; private set; }

            public Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request)
            {
                CashOutBetCalled = true;
                return Task.FromResult(new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new BetCashOutResponseDTO()
                });
            }

            public Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistorySettled(BetHistorySettledRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(BetHistoryRunningRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetConfigFEResponseDTO>> BetConfig(BetConfigRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<VerticalConfig>> UserBetConfig(UserBetConfigDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessUserBetConfig(UserBetConfigRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ResendSettlementResponseDTO>> ResendSettlement(ResendSettlementMessageRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessParlayBonus(ParlayBonusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetParlayBonus(GetParlayBonusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetStatus>> GetBetStatus(BetStatusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetRecommendationsResponseDTO>> GetBetRecommendations(BetRecommendationRequestDTO request) => throw new NotImplementedException();
        }

        // GetClient (the guardrail's client lookup, called before webprops) checks this cache
        // first; returning null from GetCache forces it through to the seeded PlatformContext.
        private sealed class NoOpMemCache : IMemCache
        {
            public void AddOrUpdateCache<T>(string key, T cacheObject, int cacheTimeout) { }
            public void AddOrUpdateCache<T>(string key, T cacheObject, DateTimeOffset date) { }
            public T? GetCache<T>(string key, Func<T> repositoryCall, int expiration) where T : class => null;
            public T? GetCache<T>(string key) where T : class => null;
            public KeyValuePair<bool, long> GetCache(string key) => default;
            public void DeleteCache(string key) { }
            public void DeleteCacheByPrefix(string prefix) { }
        }

        private static (PlatformRepository Repository, SpyWebPropsRepository WebProps, RecordingSuccessBettingRepository Betting) CreateRepository(
            Guid clientGuid, string clientCode, bool fullCashout, bool partialCashout)
        {
            var context = InMemoryPlatformContext.Create();
            context.Client.Add(new Client
            {
                ClientId = clientGuid,
                ClientCode = clientCode,
                ClientName = "Test Client",
                IsActive = true,
                Endpoint = "http://localhost",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                CurrentCompanyShare = 0m,
                NextCompanyShare = 0m,
                LimitId = "L1",
                ClientIdRef = 1,
                IsAgent = false
            });
            context.SaveChanges();

            var webProps = new SpyWebPropsRepository(new BaseResponseDTO<WebPropsConfigResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Result = new WebPropsConfigResponseDTO
                {
                    Features = new WebPropsFeaturesDTO { FullCashout = fullCashout, PartialCashout = partialCashout }
                }
            });
            var betting = new RecordingSuccessBettingRepository();

            var repository = new PlatformRepository(
                operatorRepository: new AlwaysAuthenticatesOperatorRepository(),
                bettingRepository: betting,
                sportsbookRepository: null!,
                casinoRepository: null!,
                webPropsRepository: webProps,
                platformContext: context,
                memCache: new NoOpMemCache(),
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: NullLogger<PlatformRepository>.Instance);

            return (repository, webProps, betting);
        }

        private static CashoutBetRequest AValidRequest(decimal? cashoutStakePercent) => new CashoutBetRequest
        {
            BetId = Guid.NewGuid().ToString(),
            Channel = Channel.Internet,
            IPAddress = "127.0.0.1",
            CashoutStake = 10m,
            CashoutStakePercent = cashoutStakePercent
        };

        [Fact]
        public async Task CashoutBet_LooksUpWebProps_ByClientGuid_NotClientCode()
        {
            var clientGuid = Guid.NewGuid();
            var (repository, webProps, _) = CreateRepository(clientGuid, "SOME_CLIENT_CODE", fullCashout: true, partialCashout: true);

            await repository.CashoutBet("api-key", clientGuid.ToString(), "operator-token", AValidRequest(1m), new OGClientRequest());

            Assert.Equal(clientGuid.ToString(), webProps.LastRequestedKey);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CashoutBet_FullCashoutRequest_AllowedOnlyWhenFullCashoutFlagOn(bool fullCashout, bool expectAllowed)
        {
            var clientGuid = Guid.NewGuid();
            var (repository, _, betting) = CreateRepository(clientGuid, "CODE", fullCashout, partialCashout: true);

            var response = await repository.CashoutBet("api-key", clientGuid.ToString(), "operator-token", AValidRequest(cashoutStakePercent: 1m), new OGClientRequest());

            Assert.Equal(expectAllowed, betting.CashOutBetCalled);
            if (expectAllowed)
                Assert.Equal(StatusCode.Success, response.StatusCode);
            else
                Assert.Equal(StatusCode.CashOut_Is_Unavailable, response.StatusCode);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task CashoutBet_PartialCashoutRequest_AllowedOnlyWhenPartialCashoutFlagOn(bool partialCashout, bool expectAllowed)
        {
            var clientGuid = Guid.NewGuid();
            var (repository, _, betting) = CreateRepository(clientGuid, "CODE", fullCashout: true, partialCashout);

            var response = await repository.CashoutBet("api-key", clientGuid.ToString(), "operator-token", AValidRequest(cashoutStakePercent: 0.5m), new OGClientRequest());

            Assert.Equal(expectAllowed, betting.CashOutBetCalled);
            if (expectAllowed)
                Assert.Equal(StatusCode.Success, response.StatusCode);
            else
                Assert.Equal(StatusCode.CashOut_Is_Unavailable, response.StatusCode);
        }

        [Fact]
        public async Task CashoutBet_WebPropsCallFails_DeniesCashout()
        {
            var clientGuid = Guid.NewGuid();
            var context = InMemoryPlatformContext.Create();
            context.Client.Add(new Client
            {
                ClientId = clientGuid,
                ClientCode = "CODE",
                ClientName = "Test Client",
                IsActive = true,
                Endpoint = "http://localhost",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                LimitId = "L1",
                ClientIdRef = 1
            });
            context.SaveChanges();

            var webProps = new SpyWebPropsRepository(new BaseResponseDTO<WebPropsConfigResponseDTO>
            {
                StatusCode = StatusCode.Error,
                Message = "webprops-api unavailable"
            });
            var betting = new RecordingSuccessBettingRepository();

            var repository = new PlatformRepository(
                operatorRepository: new AlwaysAuthenticatesOperatorRepository(),
                bettingRepository: betting,
                sportsbookRepository: null!,
                casinoRepository: null!,
                webPropsRepository: webProps,
                platformContext: context,
                memCache: new NoOpMemCache(),
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: NullLogger<PlatformRepository>.Instance);

            var response = await repository.CashoutBet("api-key", clientGuid.ToString(), "operator-token", AValidRequest(cashoutStakePercent: 1m), new OGClientRequest());

            Assert.False(betting.CashOutBetCalled);
            Assert.Equal(StatusCode.CashOut_Is_Unavailable, response.StatusCode);
        }
    }
}

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
    // GM-927: BettingRepository.GetBetHistory returns StatusCode.Error with a null Result when
    // Betting.API answers non-2xx (other than 400) or the call throws (e.g. a timeout). The EN
    // branch of PlatformRepository.GetBetHistory passed that through, but the non-EN (translation)
    // branch read betHistory.Result.Bets.Count and threw a NullReferenceException, turning the
    // upstream error into an unhandled 500. Non-EN callers must get the upstream response as-is.
    public class GetBetHistoryUpstreamFailureTests
    {
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

        private sealed class FixedBetHistoryBettingRepository : IBettingRepository
        {
            private readonly BaseResponseDTO<BetHistoryResponseDTO> _response;

            public FixedBetHistoryBettingRepository(BaseResponseDTO<BetHistoryResponseDTO> response)
            {
                _response = response;
            }

            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request) => Task.FromResult(_response);

            public Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistorySettled(BetHistorySettledRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(BetHistoryRunningRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetConfigFEResponseDTO>> BetConfig(BetConfigRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<VerticalConfig>> UserBetConfig(UserBetConfigDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request) => throw new NotImplementedException();
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

        // GetClient checks this cache first; returning null forces it through to the seeded PlatformContext.
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

        // sportsbookRepository is null: the translation call must not be reached on an upstream failure.
        private static PlatformRepository CreateRepository(Guid clientGuid, BaseResponseDTO<BetHistoryResponseDTO> bettingResponse)
        {
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

            return new PlatformRepository(
                operatorRepository: new AlwaysAuthenticatesOperatorRepository(),
                bettingRepository: new FixedBetHistoryBettingRepository(bettingResponse),
                sportsbookRepository: null!,
                casinoRepository: null!,
                webPropsRepository: null!,
                platformContext: context,
                memCache: new NoOpMemCache(),
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: NullLogger<PlatformRepository>.Instance);
        }

        [Theory]
        [InlineData("EN")]
        [InlineData("TH")]
        [InlineData("KO")]
        public async Task BettingServiceError_WithNullResult_IsReturnedAsIs(string langCode)
        {
            var clientGuid = Guid.NewGuid();
            var upstream = new BaseResponseDTO<BetHistoryResponseDTO>
            {
                StatusCode = StatusCode.Error,
                Message = "Error (The request was canceled due to the configured HttpClient.Timeout of 25 seconds elapsing.)"
            };
            var repository = CreateRepository(clientGuid, upstream);

            var response = await repository.GetBetHistory("api-key", clientGuid.ToString(), langCode, "operator-token", new BetHistoryRequest { IsCurrent = true }, new OGClientRequest());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(upstream.Message, response.Message);
            Assert.Null(response.Result);
        }
    }
}

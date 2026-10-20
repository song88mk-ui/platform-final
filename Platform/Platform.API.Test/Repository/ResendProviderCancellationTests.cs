using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Repository;
using Platform.API.Telemetry;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System.Text.Json;

namespace Platform.API.Test.Repository
{
    // BO-125: PlatformRepository.ResendProviderCancellation sends the provider (GTS) a Cancelled update
    // for bets that are Cancelled here but still Open there. What matters is what reaches Betting.API:
    // the OnBetCancellation message type Provider.Service handles, the provider-only routing key
    // r.bet.cancellation (r.bet.operator would also deliver a second cancellation to the operator),
    // and an OnBetSettlement payload with IsAudit set. The bet's status is read from the betting
    // database, so a bet that is not Cancelled is refused whatever the caller sends.
    public class ResendProviderCancellationTests
    {
        private sealed class RecordingBettingRepository : IBettingRepository
        {
            public Dictionary<string, ManualBetDetailsDTO> Bets { get; } = new();
            public List<ResendSettlementMessageRequestDTO> Resends { get; } = new();
            public Func<ResendSettlementMessageRequestDTO, BaseResponseDTO<ResendSettlementResponseDTO>>? ResendResponse { get; set; }
            public Exception? GetManualBetDetailsThrows { get; set; }

            public Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request)
            {
                if (GetManualBetDetailsThrows != null)
                    throw GetManualBetDetailsThrows;

                var result = new ManualBetDetailsResponseDTO();
                foreach (var betId in request.BetIds)
                    if (Bets.TryGetValue(betId, out var bet))
                        result.Bets.Add(bet);

                return Task.FromResult(new BaseResponseDTO<ManualBetDetailsResponseDTO> { StatusCode = StatusCode.Success, Result = result });
            }

            public Task<BaseResponseDTO<ResendSettlementResponseDTO>> ResendSettlement(ResendSettlementMessageRequestDTO request)
            {
                Resends.Add(request);

                if (ResendResponse != null)
                    return Task.FromResult(ResendResponse(request));

                return Task.FromResult(new BaseResponseDTO<ResendSettlementResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ResendSettlementResponseDTO
                    {
                        Data = request.Messages.Select(m => new ResendSettlementResponse { BetId = m.BetId, StatusCode = StatusCode.Success, Message = "Success" }).ToList()
                    }
                });
            }

            public Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistorySettled(BetHistorySettledRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(BetHistoryRunningRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetConfigFEResponseDTO>> BetConfig(BetConfigRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<VerticalConfig>> UserBetConfig(UserBetConfigDetailsRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessUserBetConfig(UserBetConfigRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<StatusCode>> ProcessParlayBonus(ParlayBonusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetParlayBonus(GetParlayBonusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetStatus>> GetBetStatus(BetStatusRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<BetRecommendationsResponseDTO>> GetBetRecommendations(BetRecommendationRequestDTO request) => throw new NotImplementedException();
        }

        private static (PlatformRepository Repository, RecordingBettingRepository Betting) CreateRepository()
        {
            var betting = new RecordingBettingRepository();

            var repository = new PlatformRepository(
                operatorRepository: null!,
                bettingRepository: betting,
                sportsbookRepository: null!,
                casinoRepository: null!,
                webPropsRepository: null!,
                platformContext: InMemoryPlatformContext.Create(),
                memCache: null!,
                configuration: new ConfigurationBuilder().Build(),
                messageBroker: null!,
                instrumentation: new Instrumentation(),
                logger: NullLogger<PlatformRepository>.Instance);

            return (repository, betting);
        }

        // FixtureId, MarketId and SelectionId are strings on the details DTO and parsed to long when the
        // OnBetSettlement is built, so they must be numeric.
        private static ManualBetDetailsDTO ABet(Guid betId, BetStatus status) => new ManualBetDetailsDTO
        {
            BetId = betId,
            UserId = "user-1",
            ClientId = "0a1b2c3d-0000-0000-0000-000000000001",
            ClientCode = "JEM",
            LimitId = "10939",
            Stake = 10m,
            BetStatusId = (int)status,
            BetStatusName = status.ToString(),
            Reason = "103 - Operator Error",
            Selections = new List<BetSelectionDetailsDTO>
            {
                new BetSelectionDetailsDTO
                {
                    BetId = betId,
                    BetSelectionId = Guid.NewGuid(),
                    FixtureId = "11",
                    MarketId = "22",
                    SelectionId = "33"
                }
            }
        };

        private static ResendProviderCancellationRequestDTO RequestFor(params Guid[] betIds) =>
            new ResendProviderCancellationRequestDTO { BetIds = betIds.Select(b => b.ToString()).ToList() };

        [Fact]
        public async Task CancelledBet_IsPublishedAsOnBetCancellation_OnTheProviderOnlyRoutingKey()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, BetStatus.Cancelled);

            var response = await repository.ResendProviderCancellation(RequestFor(betId));

            var message = Assert.Single(Assert.Single(betting.Resends).Messages);
            Assert.Equal("OnBetCancellation", message.MessageType);
            Assert.Equal("r.bet.cancellation", message.RoutingKey);
            Assert.Equal(betId, message.BetId);
            Assert.False(string.IsNullOrWhiteSpace(message.CorrelationId));

            Assert.Equal(StatusCode.Success, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(betId, result.BetId);
            Assert.Equal(StatusCode.Success, result.StatusCode);
        }

        [Fact]
        public async Task CancelledBet_PayloadIsTheOnBetSettlementProviderServiceExpects()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, BetStatus.Cancelled);

            await repository.ResendProviderCancellation(RequestFor(betId));

            var message = Assert.Single(Assert.Single(betting.Resends).Messages);
            var payload = JsonSerializer.Deserialize<OnBetSettlement>(message.Info)!;

            // IsAudit gates GTSBetProvider.ProcessSettlement, so a payload without it is dropped there.
            Assert.True(payload.IsAudit);
            Assert.Equal(betId, payload.Bet.BetId);
            Assert.Equal((int)BetStatus.Cancelled, payload.Bet.BetStatusId);
            Assert.Equal("JEM", payload.Bet.ClientCode);
            Assert.Equal("10939", payload.Bet.LimitId);
            var selection = Assert.Single(payload.BetSelections);
            Assert.Equal(11, selection.FixtureId);
            Assert.Equal(22, selection.MarketId);
            Assert.Equal(33, selection.SelectionId);
        }

        [Theory]
        [InlineData(BetStatus.Running)]
        [InlineData(BetStatus.Won)]
        [InlineData(BetStatus.Lost)]
        [InlineData(BetStatus.Void)]
        [InlineData(BetStatus.CashedOut)]
        public async Task BetThatIsNotCancelled_IsRefusedAndNothingIsPublished(BetStatus status)
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, status);

            var response = await repository.ResendProviderCancellation(RequestFor(betId));

            Assert.Empty(betting.Resends);
            Assert.Equal(StatusCode.Error, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Contains("not cancelled", result.Message);
            Assert.Contains(status.ToString(), result.Message);
        }

        [Fact]
        public async Task UnknownBet_ReturnsBetNotFound_AndNothingIsPublished()
        {
            var (repository, betting) = CreateRepository();

            var response = await repository.ResendProviderCancellation(RequestFor(Guid.NewGuid()));

            Assert.Empty(betting.Resends);
            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(StatusCode.Bet_Not_Found, Assert.Single(response.Result.Data).StatusCode);
        }

        [Fact]
        public async Task InvalidBetId_IsRefused_AndNothingIsPublished()
        {
            var (repository, betting) = CreateRepository();

            var response = await repository.ResendProviderCancellation(new ResendProviderCancellationRequestDTO { BetIds = new List<string> { "not-a-guid" } });

            Assert.Empty(betting.Resends);
            Assert.Equal(StatusCode.Error, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Contains("not-a-guid", result.Message);
        }

        [Fact]
        public async Task EmptyRequest_IsAnError_AndNothingIsPublished()
        {
            var (repository, betting) = CreateRepository();

            var response = await repository.ResendProviderCancellation(new ResendProviderCancellationRequestDTO());

            Assert.Empty(betting.Resends);
            Assert.Equal(StatusCode.Error, response.StatusCode);
        }

        [Fact]
        public async Task MixedBets_OnlyTheCancelledOneIsPublished_AndTheOverallResultIsAnError()
        {
            var cancelledId = Guid.NewGuid();
            var wonId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[cancelledId.ToString()] = ABet(cancelledId, BetStatus.Cancelled);
            betting.Bets[wonId.ToString()] = ABet(wonId, BetStatus.Won);

            var response = await repository.ResendProviderCancellation(RequestFor(cancelledId, wonId));

            var message = Assert.Single(Assert.Single(betting.Resends).Messages);
            Assert.Equal(cancelledId, message.BetId);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(2, response.Result.Data.Count);
            Assert.Equal(StatusCode.Success, response.Result.Data.Single(d => d.BetId == cancelledId).StatusCode);
            Assert.Equal(StatusCode.Error, response.Result.Data.Single(d => d.BetId == wonId).StatusCode);
        }

        [Fact]
        public async Task DuplicateBetIds_AreSentOnce()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, BetStatus.Cancelled);

            var response = await repository.ResendProviderCancellation(RequestFor(betId, betId));

            Assert.Single(Assert.Single(betting.Resends).Messages);
            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Single(response.Result.Data);
        }

        [Fact]
        public async Task PublishFailure_IsReportedPerBet()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, BetStatus.Cancelled);
            betting.ResendResponse = request => new BaseResponseDTO<ResendSettlementResponseDTO>
            {
                StatusCode = StatusCode.Error,
                Result = new ResendSettlementResponseDTO
                {
                    Data = request.Messages.Select(m => new ResendSettlementResponse { BetId = m.BetId, StatusCode = StatusCode.Error, Message = "broker down" }).ToList()
                }
            };

            var response = await repository.ResendProviderCancellation(RequestFor(betId));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Equal("broker down", result.Message);
        }

        [Fact]
        public async Task PublishResponseWithoutData_StillReportsEveryBetAsFailed()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.Bets[betId.ToString()] = ABet(betId, BetStatus.Cancelled);
            betting.ResendResponse = _ => new BaseResponseDTO<ResendSettlementResponseDTO> { StatusCode = StatusCode.Error, Message = "Error" };

            var response = await repository.ResendProviderCancellation(RequestFor(betId));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(betId, result.BetId);
            Assert.Equal(StatusCode.Error, result.StatusCode);
        }

        [Fact]
        public async Task BetLookupThrows_ReturnsAnError_InsteadOfFaulting()
        {
            var betId = Guid.NewGuid();
            var (repository, betting) = CreateRepository();
            betting.GetManualBetDetailsThrows = new HttpRequestException("betting api unreachable");

            var response = await repository.ResendProviderCancellation(RequestFor(betId));

            Assert.Empty(betting.Resends);
            Assert.Equal(StatusCode.Error, response.StatusCode);
        }
    }
}

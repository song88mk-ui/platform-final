using Microsoft.EntityFrameworkCore;
using Platform.Betting.API.Repository;
using Platform.Shared.Data.Betting;
using Platform.Shared.Enums;
using Platform.Shared.Models;

namespace Platform.Betting.API.Test.Repository
{
    // BO-125: ResendSettlement used to publish every message on r.bet.operator, which both
    // Operator.Servicev2 and Provider.Service bind. A message can now ask for r.bet.cancellation
    // (Provider.Service only) so a cancelled bet can be resent to the provider without a second
    // cancellation reaching the operator. A message that omits the key must still go to r.bet.operator,
    // so existing callers (Platform.API ResendSettlement) behave exactly as before, and any key outside
    // the two is refused so this endpoint cannot publish on an arbitrary routing key.
    public class ResendSettlementRoutingKeyTests
    {
        private sealed class RecordingRabbitMQRepository : IRabbitMQRepository
        {
            public List<MessageRequestDTO> Published { get; } = new();
            public StatusCode PublishStatus { get; set; } = StatusCode.Success;

            public BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request)
            {
                Published.Add(request);
                return new BaseResponseDTO<MessageResponseDTO>
                {
                    StatusCode = PublishStatus,
                    Message = PublishStatus.ToString(),
                    Result = new MessageResponseDTO { IsSuccess = PublishStatus == StatusCode.Success }
                };
            }
        }

        private static (BettingRepository Repository, RecordingRabbitMQRepository Rabbit) CreateRepository()
        {
            var rabbit = new RecordingRabbitMQRepository();
            var context = new BettingContext(new DbContextOptionsBuilder<BettingContext>()
                .UseInMemoryDatabase($"betting-{Guid.NewGuid():N}")
                .Options);

            var repository = new BettingRepository(
                bettingContext: context,
                configuration: null!,
                memCache: null!,
                httpClientFactory: null!,
                providerFactory: null!,
                sportsbookRepository: null!,
                rabbitMQRepository: rabbit,
                logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<BettingRepository>.Instance);

            return (repository, rabbit);
        }

        private static ResendSettlementMessageRequestDTO RequestWith(params ResendSettlementMessageDTO[] messages) =>
            new ResendSettlementMessageRequestDTO { Messages = messages.ToList() };

        private static ResendSettlementMessageDTO AMessage(string? routingKey, Guid? betId = null) => new ResendSettlementMessageDTO
        {
            BetId = betId ?? Guid.NewGuid(),
            MessageType = "OnBetCancellation",
            Info = "{}",
            CorrelationId = "corr-1",
            RoutingKey = routingKey!
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NoRoutingKey_PublishesOnTheOperatorKey_AsBefore(string? routingKey)
        {
            var (repository, rabbit) = CreateRepository();

            var response = await repository.ResendSettlement(RequestWith(AMessage(routingKey)));

            Assert.Equal("r.bet.operator", Assert.Single(rabbit.Published).RoutingKey);
            Assert.Equal(StatusCode.Success, response.StatusCode);
        }

        [Fact]
        public async Task ProviderCancellationKey_IsPublishedAsRequested()
        {
            var (repository, rabbit) = CreateRepository();

            var response = await repository.ResendSettlement(RequestWith(AMessage("r.bet.cancellation")));

            var published = Assert.Single(rabbit.Published);
            Assert.Equal("r.bet.cancellation", published.RoutingKey);
            Assert.Equal("OnBetCancellation", published.MessageType);
            Assert.Equal("corr-1", published.CorrelationId);
            Assert.Equal(StatusCode.Success, response.StatusCode);
        }

        [Theory]
        [InlineData("r.of.bet.cancellation")]
        [InlineData("r.oe.cachesync.ontranslationchange")]
        [InlineData("r.bet.#")]
        [InlineData("R.BET.OPERATOR")]
        public async Task AnyOtherKey_IsRefused_AndNothingIsPublished(string routingKey)
        {
            var (repository, rabbit) = CreateRepository();
            var betId = Guid.NewGuid();

            var response = await repository.ResendSettlement(RequestWith(AMessage(routingKey, betId)));

            Assert.Empty(rabbit.Published);
            Assert.Equal(StatusCode.Error, response.StatusCode);
            var result = Assert.Single(response.Result.Data);
            Assert.Equal(betId, result.BetId);
            Assert.Equal(StatusCode.Error, result.StatusCode);
            Assert.Equal("Routing key is not allowed", result.Message);
        }

        [Fact]
        public async Task RefusedMessage_DoesNotStopTheOthersInTheSameRequest()
        {
            var (repository, rabbit) = CreateRepository();
            var refusedId = Guid.NewGuid();
            var okId = Guid.NewGuid();

            var response = await repository.ResendSettlement(RequestWith(AMessage("r.bad.key", refusedId), AMessage("r.bet.cancellation", okId)));

            Assert.Equal("r.bet.cancellation", Assert.Single(rabbit.Published).RoutingKey);
            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(StatusCode.Error, response.Result.Data.Single(d => d.BetId == refusedId).StatusCode);
            Assert.Equal(StatusCode.Success, response.Result.Data.Single(d => d.BetId == okId).StatusCode);
        }

        [Fact]
        public async Task PublishFailure_IsStillReportedAsAnError()
        {
            var (repository, rabbit) = CreateRepository();
            rabbit.PublishStatus = StatusCode.Error;

            var response = await repository.ResendSettlement(RequestWith(AMessage("r.bet.cancellation")));

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal(StatusCode.Error, Assert.Single(response.Result.Data).StatusCode);
        }
    }
}

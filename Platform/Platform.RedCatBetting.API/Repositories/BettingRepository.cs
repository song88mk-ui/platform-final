using Microsoft.IdentityModel.Tokens;
using Platform.Shared.Data.RedCatBetting;
using Platform.Shared.Data.RedCatBetting.Entities;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Repositories
{
    public class BettingRepository : IBettingRepository
    {
        private readonly RedCatBettingContext _redCatBettingContext;
        private readonly IOperatorRepository _operatorRepository;

        public BettingRepository(RedCatBettingContext redCatBettingContext, IOperatorRepository operatorRepository)
        {
            _redCatBettingContext = redCatBettingContext;
            _operatorRepository = operatorRepository;
        }
        public async Task<BaseResponseDTO<StakeResponseDTO>> PlaceBet(RedCatBetRequest request)
        {
            var betEntity = _redCatBettingContext.Bet.Add(new Bet()
            {
                ClientId = request.ClientId,
                ClientCode = request.ClientCode,
                Endpoint = request.Endpoint,
                OperatorToken = request.OperatorToken,
                CurrencyCode = request.Currency,
                GameId = request.GameId,
                BetMarketId = request.BetMarketId,
                Stake = request.TransactionAmt,
                BetDate = request.EventDate,
                BetIdRef = request.BetId.ToString()
            }).Entity;

            var betAuditEntity = _redCatBettingContext.BetAudit.Add(new BetAudit()
            {
                TransactionId = request.TransactionId,
                BetId = betEntity.BetId,
                CLientId = betEntity.ClientId,
                ClientCode = betEntity.ClientCode,
                Endpoint = betEntity.Endpoint,
                OperatorToken = betEntity.OperatorToken,
                CurrencyCode = betEntity.CurrencyCode,
                GameId = betEntity.GameId,
                BetMarketId = betEntity.BetMarketId,
                Stake = betEntity.Stake,
                BetDate = betEntity.BetDate,
                BetIdRef = betEntity.BetIdRef,
                Timestamp = DateTime.UtcNow
            }).Entity;

            var operatorResponse = await _operatorRepository.ProcessBetPlacement(new RedCatBetRequest()
            {
                BetId = request.BetId,
                ClientId = request.ClientId,
                ClientCode = request.ClientCode,
                Endpoint = request.Endpoint,
                OperatorToken = request.OperatorToken,
                Currency = request.Currency,
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId,
                EventDate = request.EventDate,
                BetMarketId = request.BetMarketId,
                TransactionAmt = request.TransactionAmt,
                TransactionId = request.TransactionId,
                GameId = request.GameId,
                CorrelationId = ConvertToEpochTimestamp(DateTime.UtcNow).ToString(),
            });

            return new BaseResponseDTO<StakeResponseDTO>()
            {
                StatusCode = operatorResponse.StatusCode,
                Message = operatorResponse.Message,
                Result = new StakeResponseDTO()
                {
                    StatusCode = operatorResponse.Result.StatusCode,
                    Balance = operatorResponse.Result.Balance,
                    TransactionRefId = betAuditEntity.Id.ToString()
                }
            };
        }
        private long ConvertToEpochTimestamp(DateTime date) => new DateTimeOffset(date).ToUnixTimeMilliseconds();
    }
}

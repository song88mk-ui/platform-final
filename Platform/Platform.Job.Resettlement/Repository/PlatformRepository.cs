using Microsoft.Extensions.Logging;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Platform.Job.Resettlement.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly ILogger<PlatformRepository> _logger;

        public PlatformRepository(ILogger<PlatformRepository> logger)
        {
            _logger = logger;
        }

        public void ProcessBets(List<OperatorSettlementRetry> bets, IMessageBroker messageBroker, CancellationToken cancellationToken)
        {
            foreach (var bet in bets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var payload = ReadPayload(bet);
                if (payload == null)
                    continue;

                payload.IsRetry = true;

                if (string.IsNullOrEmpty(payload.Bet.BetIdRef))
                    messageBroker.Publish(payload, bet.MessageType, "r.bet.operator", bet.CorrelationId);
            }
        }

        // A row whose Info cannot be read can never be replayed, so it is skipped instead of
        // stopping the rest of the batch. Info is not logged: it carries the operator payload.
        private OnBetSettlement ReadPayload(OperatorSettlementRetry bet)
        {
            OnBetSettlement payload = null;
            Exception failure = null;

            try
            {
                payload = JsonSerializer.Deserialize<OnBetSettlement>(bet.Info);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentNullException)
            {
                failure = ex;
            }

            if (payload?.Bet != null)
                return payload;

            _logger.LogError(failure, "skipping retry row with no readable bet, BetId {BetId}, CorrelationId {CorrelationId}", bet.BetId, bet.CorrelationId);
            return null;
        }
    }
}

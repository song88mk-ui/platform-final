using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System.Collections.Generic;
using System.Text.Json;

namespace Platform.Job.Resettlement.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        public void ProcessBets(List<OperatorSettlementRetry> bets, IMessageBroker messageBroker)
        {
            foreach (var bet in bets)
            {
                var payload = JsonSerializer.Deserialize<OnBetSettlement>(bet.Info);

                messageBroker.Publish(payload, bet.MessageType, "r.bet.operator", bet.CorrelationId);
            }
        }
    }
}

using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Utility;
using System.Collections.Generic;
using System.Threading;

namespace Platform.Job.Resettlement.Repository
{
    public interface IPlatformRepository
    {
        void ProcessBets(List<OperatorSettlementRetry> bets, IMessageBroker messageBroker, CancellationToken cancellationToken);
    }
}

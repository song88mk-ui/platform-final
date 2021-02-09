using Platform.Placement.Service.Data;
using Platform.Placement.Service.Utility;
using Platform.Shared.MessageModels;

namespace Platform.Placement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly BettingContext _bettingContext;
        public BettingRepository(BettingContext bettingContext)
        {
            _bettingContext = bettingContext;
        }
        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {
            //var bet = _bettingContext.Bet.Find(message)
        }
    }
}

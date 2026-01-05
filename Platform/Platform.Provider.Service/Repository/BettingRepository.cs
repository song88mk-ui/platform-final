using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Shared.Providers;

namespace Platform.Provider.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private ILogger<BettingRepository> _logger;
        public BettingRepository(IConfiguration configuration, ILogger<BettingRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public void ProcessTimeout(OnBetTimeout message, IBetProvider provider, string correlationId)
        {
            provider.ProcessCancellation(message, correlationId, false).GetAwaiter().GetResult();
        }

        public void ProcessBackofficeCancellation(OnBetBackofficeCancellation message, IBetProvider provider, string correlationId)
        {
            provider.ProcessCancellation(new OnBetTimeout
            {
                BetId = message.BetId,
            }, correlationId, true).GetAwaiter().GetResult();
        }
    }
}

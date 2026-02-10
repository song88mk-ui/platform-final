using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using System;

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

        public void ProcessBetCancellation(Guid betId, string limitId, string clientCode, IBetProvider provider, string correlationId, bool isBackOfficeTrigger = false)
        {
            provider.ProcessCancellation(betId, limitId, clientCode, correlationId, isBackOfficeTrigger);
        }

        public void ProcessBetSettlement(OnBetSettlement message, IBetProvider provider, string correlationId)
        {
            provider.ProcessSettlement(message, correlationId);
        }
    }
}

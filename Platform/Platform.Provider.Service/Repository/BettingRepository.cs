using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.Enums;
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

        public void ProcessBetCashout(OnBetSettlement message, CashoutType cashoutType, IBetProvider provider, string correlationId)
        {
            // Block until the provider call completes: the worker's DI scope (and the provider's
            // HttpClient) is disposed as soon as the message handler returns, so a fire-and-forget
            // call would be cancelled mid-flight and the GTS update silently lost.
            provider.ProcessCashout(message, cashoutType, correlationId).GetAwaiter().GetResult();
        }
    }
}

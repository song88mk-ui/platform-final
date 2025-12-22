using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Shared.Providers;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Text.Json;
using System.Threading;

namespace Platform.Cancellation.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private IBuilderFactory _factory;
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

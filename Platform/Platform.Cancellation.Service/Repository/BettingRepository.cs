using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Text.Json;

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
        public void ProcessMTSTimeout(OnBetTimeout message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    _factory = mts.BuilderFactory;

                    var betCancel = _factory.CreateTicketCancelBuilder().BuildTicket(message.BetId.ToString().ToUpper(), Convert.ToInt32(_configuration["Custom:BookmakerId"]), TicketCancellationReason.TimeoutTriggered);

                    var payload = new
                    {
                        message = JsonSerializer.Serialize(message),
                        payload = JsonSerializer.Serialize(betCancel)
                    };

                    _logger.LogInformation($"ProcessMTSTimeout (Request)- {payload}");
                    var response = mts.SendTicketCancelBlocking(betCancel);
                    _logger.LogInformation($"ProcessMTSTimeout (Response)- {payload}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        public void ProcessBackofficeCancellation(OnBetBackofficeCancellation message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    _factory = mts.BuilderFactory;

                    var betCancel = _factory.CreateTicketCancelBuilder().BuildTicket(message.BetId.ToString().ToUpper(), Convert.ToInt32(_configuration["Custom:BookmakerId"]), TicketCancellationReason.BookmakerBackofficeTriggered);

                    var response = mts.SendTicketCancelBlocking(betCancel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }
    }
}

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Cancellation.Service.Utility;
using RabbitMQ.Client.Events;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;

namespace Platform.Cancellation.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private MtsSdk _mtsSdk;
        private IBuilderFactory _factory;
        private const int BookmakerId = 32290; // 25740 is original value, 32290 is for 10 bet

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker)
        {
            _logger = logger;
            _messageBroker = messageBroker;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var config = MtsSdk.GetConfiguration();
            _mtsSdk = new MtsSdk(config);
            _mtsSdk.Open();

            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _mtsSdk.Close();
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;

            try
            {
                switch (messageType)
                {
                    case "timeout":
                        var cancellation = JsonSerializer.Deserialize<OnBetCancellation>(payload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        _factory = _mtsSdk.BuilderFactory;

                        var betCancel = _factory.CreateTicketCancelBuilder().BuildTicket(cancellation.BetId.ToString().ToUpper(), BookmakerId, TicketCancellationReason.TimeoutTriggered);

                        var timeoutResponse = _mtsSdk.SendTicketCancelBlocking(betCancel);

                        if (timeoutResponse.Status == TicketCancelAcceptance.NotCancelled)
                            _logger.LogInformation($"TicketId = {cancellation.BetId.ToString().ToUpper()} Reason = Contact OAM");
                        else
                            _logger.LogInformation($"TicketId = {cancellation.BetId.ToString().ToUpper()} Reason = 102 Successful");
                        break;
                    case "manual.cancellation":
                        var manualCancellation = JsonSerializer.Deserialize<OnBetCancellation>(payload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        _factory = _mtsSdk.BuilderFactory;

                        var manualBetCancel = _factory.CreateTicketCancelBuilder().BuildTicket(manualCancellation.BetId.ToString().ToUpper(), BookmakerId, TicketCancellationReason.BookmakerBackofficeTriggered);

                        var manualResponse = _mtsSdk.SendTicketCancelBlocking(manualBetCancel);

                        if (manualResponse.Status == TicketCancelAcceptance.NotCancelled)
                        {
                            manualResponse.Acknowledge();
                            _logger.LogInformation($"TicketId = {manualCancellation.BetId.ToString().ToUpper()} Reason = Contact OAM");
                        }
                        else
                        {
                            manualResponse.Acknowledge();
                            _logger.LogInformation($"TicketId = {manualCancellation.BetId.ToString().ToUpper()} Reason = 103 Successful");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(messageType, ex);
            }
        }
    }
}

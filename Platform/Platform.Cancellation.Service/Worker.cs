using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
                        var cancellation = JsonConvert.DeserializeObject<BetCancellationPayload>(payload);

                        _factory = _mtsSdk.BuilderFactory;

                        var betCancel = _factory.CreateTicketCancelBuilder().BuildTicket(cancellation.BetId.ToString().ToUpper(), 25740, TicketCancellationReason.TimeoutTriggered);

                        var timeoutResponse = _mtsSdk.SendTicketCancelBlocking(betCancel);

                        if (timeoutResponse.Status == TicketCancelAcceptance.NotCancelled)
                            _logger.LogInformation($"TicketId = {cancellation.BetId.ToString().ToUpper()} Reason = Contact OAM");
                        else
                            _logger.LogInformation($"TicketId = {cancellation.BetId.ToString().ToUpper()} Reason = 102 Successful");
                        break;
                    case "manual.cancellation":
                        var manualCancellation = JsonConvert.DeserializeObject<BetCancellationPayload>(payload);

                        _factory = _mtsSdk.BuilderFactory;

                        var manualBetCancel = _factory.CreateTicketCancelBuilder().BuildTicket(manualCancellation.BetId.ToString().ToUpper(), 25740, TicketCancellationReason.BookmakerBackofficeTriggered);

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

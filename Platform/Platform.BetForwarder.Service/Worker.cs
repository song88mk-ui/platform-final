using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.BetForwarder.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IMessageBroker _messageBrokerExternal;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IMessageBroker messageBrokerExternal)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _messageBrokerExternal = messageBrokerExternal;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBrokerExternal.Dispose();

            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _messageBrokerExternal.ExternalConnect();

            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }
        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);
                        //

                        _messageBrokerExternal.ExternalPublish(onBetPlacement, messageType, e.RoutingKey, correlationId);
                        break;

                    case "OnBetRejection":
                        var onBetRejection = JsonSerializer.Deserialize<OnBetPlacement>(payload);

                        _messageBrokerExternal.ExternalPublish(onBetRejection, messageType, e.RoutingKey, correlationId);
                        break;

                    case "OnBetCancellation":
                        var onBetCancellation = JsonSerializer.Deserialize<OnBetSettlement>(payload);

                        _messageBrokerExternal.ExternalPublish(onBetCancellation, messageType, e.RoutingKey, correlationId);
                        break;

                    case "OnBetFullCashOut":
                        var onBetFullCashOut = JsonSerializer.Deserialize<OnBetSettlement>(payload);

                        _messageBrokerExternal.ExternalPublish(onBetFullCashOut, messageType, e.RoutingKey, correlationId);
                        break;

                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnBetSettlement>(payload);

                        _messageBrokerExternal.ExternalPublish(onBetSettlement, messageType, e.RoutingKey, correlationId);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}

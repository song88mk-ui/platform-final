using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.BetStatus.Service.Utility;
using Platform.Shared.MessageModels;
using RabbitMQ.Client.Events;

namespace Platform.BetStatus.Service
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
                    case "OnBetStatus":
                        var onBetStatus = JsonSerializer.Deserialize<OnBetStatus>(payload);

                        _messageBrokerExternal.ExternalPublish(onBetStatus, "OnBetStatus", $"r.bet.onbetstatus.{onBetStatus.ClientId}", correlationId);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}

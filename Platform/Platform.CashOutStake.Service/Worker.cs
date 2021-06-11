using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.CashOutStake.Service.Repository;
using Platform.Shared.Enums;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.CashOutStake.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IMessageBroker _messageBrokerExternal;
        private readonly IBettingRepository _bettingRepository;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IMessageBroker messageBrokerExternal, IBettingRepository bettingRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _messageBrokerExternal = messageBrokerExternal;
            _bettingRepository = bettingRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //_messageBrokerExternal.Dispose();

            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //_messageBrokerExternal.ExternalConnect();

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
                    case "OnOddsChange":
                        var onOddsChange = JsonSerializer.Deserialize<OnInternalOddsChangeMessage>(payload);

                        _bettingRepository.ProcessCashOutStake(onOddsChange);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class OnInternalOddsChangeMessage
    {
        public int SportId { get; set; }
        public long FixtureId { get; set; }
        public FixtureStatus FixtureStatus { get; set; }
        public FixtureType FixtureType { get; set; }
    }
}

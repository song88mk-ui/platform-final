using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Platform.Settlement.Service.Utility;
using System.Text.Json;
using System.Text;
using Platform.Placement.Service.Entities;
using Platform.Settlement.Service.Repository;

namespace Platform.Settlement.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IPlatformRepository _platformRepository;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IPlatformRepository platformRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _platformRepository = platformRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

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
                    case "won2":
                        var onSettlement = JsonSerializer.Deserialize<OnSettlementMessage>(payload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        _platformRepository.ProcessSettlement(onSettlement, _messageBroker);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}

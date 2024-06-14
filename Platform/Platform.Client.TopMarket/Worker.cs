using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.ClientTopMarket.Service.Repository;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.ClientTopMarket.Service
{
    //
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        private Thread _threadBetPlacement;
        private Thread _threadBetSettlement;

        private readonly BlockingCollection<OnClientTopMarketBetPlacement> _messagesBetPlacement = new BlockingCollection<OnClientTopMarketBetPlacement>();
        private readonly BlockingCollection<OnClientTopMarketBetSettlement> _messagesBetSettlement = new BlockingCollection<OnClientTopMarketBetSettlement>();
        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
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
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            _threadBetPlacement = new Thread(ThreadBetPlacement);
            _threadBetSettlement = new Thread(ThreadBetSettlement);

            _threadBetPlacement.Start();
            _threadBetSettlement.Start();

            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private async void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnClientTopMarketBetPlacement>(payload);
                        if (onBetPlacement.SportId < 1000 && onBetPlacement.MarketSelectionCount <= 3 && onBetPlacement.FixtureTypeId != (int)FixtureType.Outright)
                            _messagesBetPlacement.Add(onBetPlacement);
                        break;
                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnClientTopMarketBetSettlement>(payload);
                        if (onBetSettlement.SportId < 1000)
                            _messagesBetSettlement.Add(onBetSettlement);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        }

        private async void ThreadBetPlacement()
        {
            while (true)
            {
                var message = _messagesBetPlacement.Take();
                await _bettingRepository.ProcessBetPlacement(message);
            }
        }
        private async void ThreadBetSettlement()
        {
            while (true)
            {
                var message = _messagesBetSettlement.Take();
                await _bettingRepository.ProcessBetSettlement(message);
            }
        }
    }
}

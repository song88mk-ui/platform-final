using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform.Shared.Utility;
using Platform.AgentReports.Service.Repository;
using RabbitMQ.Client.Events;
using Platform.Shared.MessageModels;

namespace Platform.AgentReports.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IReportsRepository _reportsRepository;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IReportsRepository reportsRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _reportsRepository = reportsRepository;
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
            _messageBroker.ConnectWithManualAck();
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
                {//
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);
                        _reportsRepository.ProcessBetPlacement(onBetPlacement);
                        break;
                    case "OnBetFullCashOut":
                        var onBetFullCashOut = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _reportsRepository.ProcessBetFullCashOut(onBetFullCashOut);
                        break;
                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _reportsRepository.ProcessBetSettlement(onBetSettlement);
                        break;
                    case "OnBetCancellation":
                        var onBetCancellation = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _reportsRepository.ProcessBetCancellation(onBetCancellation);
                        break;
                    case "OnCasinoBetPlacement":
                        var onCasinoBetPlacement = JsonSerializer.Deserialize<OnCasinoBetPlacement>(payload);
                        _reportsRepository.ProcessCasinoBetPlacement(onCasinoBetPlacement);
                        break;
                    case "OnCasinoBetSettlement":
                        var onCasinoBetSettlement = JsonSerializer.Deserialize<OnCasinoBetSettlement>(payload);
                        _reportsRepository.ProcessBetSettlement(onCasinoBetSettlement);
                        break;
                }

                _messageBroker.Acknowledge(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
    }
}

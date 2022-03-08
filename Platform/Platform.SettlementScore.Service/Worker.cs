using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.SettlementScore.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.SettlementScore.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

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
            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            _bettingRepository.PreloadActiveBets();

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
                    //case "OnOddsChange":
                    //    var onOddsChange = JsonSerializer.Deserialize<OnExternalOddsChangeMessage>(payload);
                    //    _bettingRepository.ProcessOddsChange(onOddsChange, _messageBroker);
                    //    break;

                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);
                        _bettingRepository.AddBet(onBetPlacement);
                        break;

                    case "OnBetCancellation":
                        var onBetCancellation = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _bettingRepository.ProcessBet(onBetCancellation);
                        break;

                    case "OnBetSettlement":
                        var onBetSettlement = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _bettingRepository.ProcessBet(onBetSettlement);
                        break;

                    case "OnBetFullCashOut":
                        var onBetFullCashOut = JsonSerializer.Deserialize<OnBetSettlement>(payload);
                        _bettingRepository.ProcessBet(onBetFullCashOut);
                        break;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}

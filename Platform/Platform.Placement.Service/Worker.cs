using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Placement.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using Sportradar.MTS.SDK.API;

namespace Platform.Placement.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IBettingRepository _bettingRepository;

        private IMtsSdk _mts;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IBettingRepository bettingRepository, IConfiguration configuration)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _bettingRepository = bettingRepository;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    var config = MtsSdk.GetConfiguration();

                    _mts = new MtsSdk(config);
                    _mts.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]))
            {
                try
                {
                    _mts.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

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
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnBetPlacement":
                        var onBetPlacement = JsonSerializer.Deserialize<OnBetPlacement>(payload);

                        _bettingRepository.ProcesssBetPlacement(onBetPlacement, _mts, _messageBroker, correlationId);
                        break;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}

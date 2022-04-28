using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Shared.Utility;
using Platform.WebSocket.API.Messages;
using Platform.WebSocket.API.Repository;
using RabbitMQ.Client.Events;

namespace Platform.WebSocket.API
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWebSocketRepository _webSocketRepository;
        private readonly IMessageBroker _messageBroker;
        private readonly IHubContext<WebSocketRepository, IPlatformEvent> _platformHub;

        public Worker(ILogger<Worker> logger, IWebSocketRepository webSocketRepository, IMessageBroker messageBroker, IHubContext<WebSocketRepository, IPlatformEvent> platformHub)
        {
            _logger = logger;
            _webSocketRepository = webSocketRepository;
            _messageBroker = messageBroker;
            _platformHub = platformHub;
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

        private async void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var clientCode = e.RoutingKey.Split('.')[4].ToLower();

            try
            {
                switch (messageType)
                {
                    case "OnOddsChange":
                        var onOddsChange = JsonSerializer.Deserialize<OutgoingOddsChange>(payload);
                        await _webSocketRepository.PublishOddsChange(_platformHub, clientCode, onOddsChange);
                        break;

                    case "OnBetStopFixture":
                        var onBetStopFixture = JsonSerializer.Deserialize<OutgoingBetStopFixture>(payload);
                        await _webSocketRepository.PublishBetStopFixture(_platformHub, clientCode, onBetStopFixture);
                        break;

                    case "OnBetStopGlobal":
                        var onBetStopGlobal = JsonSerializer.Deserialize<OutgoingBetStopGlobal>(payload);
                        await _webSocketRepository.PublishBetStopGlobal(_platformHub, clientCode, onBetStopGlobal);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}

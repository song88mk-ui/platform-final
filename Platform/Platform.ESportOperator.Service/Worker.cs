using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.ESportOperator.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.ESportOperator.Service
{
    public class Worker : BackgroundService
    {
        #region Fields

        private readonly ILogger<Worker> _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IPlatformRepository _platformRepository;

        #endregion

        #region Ctor

        public Worker(ILogger<Worker> logger, IMessageBroker messageBroker, IPlatformRepository platformRepository)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _platformRepository = platformRepository;
        }

        #endregion

        #region Methods

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

        #endregion

        #region Utilities

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            //
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            var correlationId = e.BasicProperties.CorrelationId;

            try
            {
                switch (messageType)
                {
                    case "OnESportDebitRequest":
                        var placementRequest = JsonSerializer.Deserialize<OnESportDebitRequest>(payload);
                        _platformRepository.ProcessESportDebitRequest(placementRequest, correlationId);
                        break;
                    case "OnESportDebitResponse":
                        var placementResponse = JsonSerializer.Deserialize<OnESportDebitResponse>(payload);
                        _platformRepository.ProcessESportDebitResponse(placementResponse, correlationId);
                        break;
                    case "OnESportCreditRequest":
                        var settlementRequest = JsonSerializer.Deserialize<OnESportCreditRequest>(payload);
                        _platformRepository.ProcessESportCreditRequest(settlementRequest, correlationId);
                        break;
                    case "OnESportCreditResponse":
                        var settlementResponse = JsonSerializer.Deserialize<OnESportCreditResponse>(payload);
                        _platformRepository.ProcessESportCreditResponse(settlementResponse, correlationId);
                        break;
                }

                _messageBroker.Acknowledge(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow} {ex.Message}");

                var error = new
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    Payload = payload
                };

                _messageBroker.Publish(JsonSerializer.Serialize(error), $"{messageType}Error", "r.bet.error.esportoperator", Guid.NewGuid().ToString());

                _messageBroker.Acknowledge(e);
            }
        }

        #endregion
    }
}

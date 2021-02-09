using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace Platform.Operator.Service.Utility
{
    public class MessageBroker : IDisposable, IMessageBroker
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumer;
        public event EventHandler<BasicDeliverEventArgs> MessageReceived;

        private readonly IConfiguration _configuration;

        private string _hostName = string.Empty;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _virtualHost = string.Empty;
        private string _exchange = string.Empty;
        private string _queue = string.Empty;
        private string _consumerName = string.Empty;
        private string _routingKey = string.Empty;

        public MessageBroker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Connect()
        {
            _hostName = _configuration["MessageBroker:Host"];
            _userName = _configuration["MessageBroker:Username"];
            _password = _configuration["MessageBroker:Password"];
            _virtualHost = _configuration["MessageBroker:VirtualHost"];
            _exchange = _configuration["MessageBroker:Exchange"];
            _queue = _configuration["MessageBroker:Queue"];
            _consumerName = _configuration["MessageBroker:Consumer"];
            _routingKey = _configuration["MessageBroker:RoutingKey"];

            if (_connection == null)
            {
                if (_connectionFactory == null)
                    _connectionFactory = new ConnectionFactory
                    {
                        HostName = _hostName,
                        Port = 5672,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                    };

                _connection = _connectionFactory.CreateConnection();

                if (_model == null)
                    _model = _connection.CreateModel();

                if (!string.IsNullOrEmpty(_queue))
                {
                    _model.ExchangeDeclare(_exchange, "topic", true, false, null);
                    _model.QueueDeclare(_queue, true, false, false, null);

                    foreach (var routingKey in _routingKey.Split('|'))
                        _model.QueueBind(_queue, _exchange, routingKey.ToLower(), null);

                    _model.BasicQos(0, 1000, false);
                    _consumer = new EventingBasicConsumer(_model);
                    _consumer.Received += HandleMessage;

                    _model.BasicConsume(_queue, false, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                }
            }
        }

        private void HandleMessage(object sender, BasicDeliverEventArgs e)
        {
            try
            {   
                MessageReceived(sender, e);

                _consumer.Model.BasicAck(e.DeliveryTag, false);
            }
            catch (Exception)
            {
                _consumer.Model.BasicNack(e.DeliveryTag, false, true);
            }
        }

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            var properties = _model.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
        }

        public void Publish(string payload, string messageType, string routingKey, string correlationId)
        {
            var properties = _model.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
        }

        public void Dispose()
        {
            _model.Dispose();
            _connection.Dispose();
        }
    }
}

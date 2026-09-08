using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Platform.Shared.Utility
{
    public class MessageBroker : IDisposable, IMessageBroker
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _consumer;
        public event EventHandler<BasicDeliverEventArgs> MessageReceived;

        private readonly IConfiguration _configuration;
        private readonly ILogger<MessageBroker> _logger;

        private readonly object _lock = new object();

        private string _hostName = string.Empty;
        private int _hostPort = 5672;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _virtualHost = string.Empty;
        private string _exchange = string.Empty;
        private string _queue = string.Empty;
        private string _consumerName = string.Empty;
        private string _routingKey = string.Empty;
        private ushort _prefetchCount = 1000;
        private bool _isExclusive = false;
        private bool _isPriority = false;

        private ConnectionFactory _externalConnectionFactory;
        private IConnection _externalConnection;
        private IModel _externalModel;

        private string _externalHostName = string.Empty;
        private int _externalHostPort = 5672;
        private string _externalUserName = string.Empty;
        private string _externalPassword = string.Empty;
        private string _externalVirtualHost = string.Empty;
        private string _externalExchange = string.Empty;
        private string _externalQueue = string.Empty;
        private string _externalConsumerName = string.Empty;
        private string _externalRoutingKey = string.Empty;

        private static readonly ActivitySource _activitySource = new ActivitySource(nameof(MessageBroker));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public MessageBroker(IConfiguration configuration, ILogger<MessageBroker> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Acknowledge(BasicDeliverEventArgs e)
        {
            _consumer.Model.BasicAck(e.DeliveryTag, false);
        }

        public void NAcknowledge(BasicDeliverEventArgs e)
        {
            _consumer.Model.BasicNack(e.DeliveryTag, false, true);
        }

        public void Connect()
        {
            _isExclusive = _configuration.GetValue<bool>("MessageBroker:IsExclusive", false);
            _hostName = _configuration["MessageBroker:Host"];
            _hostPort = _configuration.GetValue<int>("MessageBroker:Port", 5672);
            _userName = _configuration["MessageBroker:Username"];
            _password = _configuration["MessageBroker:Password"];
            _virtualHost = _configuration["MessageBroker:VirtualHost"];
            _exchange = _configuration["MessageBroker:Exchange"];
            _consumerName = _configuration["MessageBroker:Consumer"];
            _routingKey = _configuration["MessageBroker:RoutingKey"];
            _prefetchCount = _configuration.GetValue<ushort>("MessageBroker:PrefetchCount", 1000);

            var clientProperties = new Dictionary<string, object>
            {
                { "host_name", Environment.MachineName },
                { "application_name", "Song88 Platform" }
            };

            var suffix = "";

            if (_isExclusive)
            {
                using (MD5 hasher = MD5.Create())
                {
                    byte[] bytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in bytes)
                        sb.Append(b.ToString("x2"));
                    suffix = sb.ToString().Substring(0, 5);
                }
            }

            _queue = _isExclusive == true ? $"{_configuration["MessageBroker:Queue"]}.{suffix}" : _configuration["MessageBroker:Queue"];

            try
            {
                if (_connection == null)
                {
                    if (_connectionFactory == null)
                        _logger.LogInformation("Connecting to RabbitMQ on [{host}:{port}; TlsEnabled: {tlsEnabled}",
                                _hostName, _hostPort, _hostPort == 5671 ? true : false);

                    _connectionFactory = new ConnectionFactory
                    {
                        HostName = _hostName,
                        Port = _hostPort,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                        Ssl = new SslOption { Enabled = _hostPort == 5671 ? true : false, ServerName = _hostName },
                        ClientProperties = clientProperties
                    };

                    _connection = _connectionFactory.CreateConnection();

                    if (_model == null)
                        _model = _connection.CreateModel();

                    if (!string.IsNullOrEmpty(_queue))
                    {
                        _model.ExchangeDeclare(_exchange, "topic", true, false, null);

                        if (_isExclusive)
                            _model.QueueDeclare(_queue, true, true, true, null);
                        else
                            _model.QueueDeclare(_queue, true, false, false, null);

                        foreach (var routingKey in _routingKey.Split('|'))
                            _model.QueueBind(_queue, _exchange, routingKey.ToLower(), null);

                        _model.BasicQos(0, _prefetchCount, false);
                        _consumer = new EventingBasicConsumer(_model);
                        _consumer.Received += HandleMessage;

                        _model.BasicConsume(_queue, false, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to establish connection to RabbitMQ!: {@exception}", e);
                throw;
            }
        }

        public void ExternalConnect()
        {
            _externalHostName = _configuration["ExternalMessageBroker:Host"];
            _externalHostPort = _configuration.GetValue<int>("ExternalMessageBroker:Port", 5672);
            _externalUserName = _configuration["ExternalMessageBroker:Username"];
            _externalPassword = _configuration["ExternalMessageBroker:Password"];
            _externalVirtualHost = _configuration["ExternalMessageBroker:VirtualHost"];
            _externalExchange = _configuration["ExternalMessageBroker:Exchange"];
            _externalQueue = _configuration["ExternalMessageBroker:Queue"];
            _externalConsumerName = _configuration["ExternalMessageBroker:Consumer"];
            _externalRoutingKey = _configuration["ExternalMessageBroker:RoutingKey"];

            _logger.LogInformation("Connecting to External RabbitMQ on [{host}:{port}; TlsEnabled: {tlsEnabled}",
                    _externalHostName, _externalHostPort, _externalHostPort == 5671 ? true : false);

            var clientProperties = new Dictionary<string, object>
            {
                { "application_name", "Song88 Platform" }
            };

            try
            {
                if (_externalConnection == null)
                {
                    if (_externalConnectionFactory == null)
                        _externalConnectionFactory = new ConnectionFactory
                        {
                            HostName = _externalHostName,
                            Port = _externalHostPort,
                            UserName = _externalUserName,
                            Password = _externalPassword,
                            VirtualHost = _externalVirtualHost,
                            RequestedHeartbeat = 60,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                            Ssl = new SslOption { Enabled = _externalHostPort == 5671 ? true : false, ServerName = _externalHostName },
                            ClientProperties = clientProperties
                        };

                    _externalConnection = _externalConnectionFactory.CreateConnection();

                    if (_externalModel == null)
                        _externalModel = _externalConnection.CreateModel();
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to establish connection to RabbitMQ!: {@exception}", e);
                throw;
            }
        }

        public void ConnectWithManualAck()
        {
            _hostName = _configuration["MessageBroker:Host"];
            _hostPort = _configuration.GetValue<int>("MessageBroker:Port", 5672);
            _userName = _configuration["MessageBroker:Username"];
            _password = _configuration["MessageBroker:Password"];
            _virtualHost = _configuration["MessageBroker:VirtualHost"];
            _exchange = _configuration["MessageBroker:Exchange"];
            _queue = _configuration["MessageBroker:Queue"];
            _consumerName = _configuration["MessageBroker:Consumer"];
            _routingKey = _configuration["MessageBroker:RoutingKey"];
            _prefetchCount = _configuration.GetValue<ushort>("MessageBroker:PrefetchCount", 1000);
            _isPriority = _configuration.GetValue<bool>("MessageBroker:IsPriority", false);

            var clientProperties = new Dictionary<string, object>
            {
                { "host_name", Environment.MachineName },
                { "application_name", "Song88 Platform" }
            };

            try
            {
                if (_connection == null)
                {
                    if (_connectionFactory == null)
                        _logger.LogInformation("Connecting to RabbitMQ on [{host}:{port}; TlsEnabled: {tlsEnabled}; ManualAck",
                                _hostName, _hostPort, _hostPort == 5671 ? true : false);

                    _connectionFactory = new ConnectionFactory
                    {
                        HostName = _hostName,
                        Port = _hostPort,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                        Ssl = new SslOption { Enabled = _hostPort == 5671 ? true : false, ServerName = _hostName },
                        ClientProperties = clientProperties,
                    };

                    _connection = _connectionFactory.CreateConnection();

                    if (_model == null)
                        _model = _connection.CreateModel();

                    if (!string.IsNullOrEmpty(_queue))
                    {
                        _model.ExchangeDeclare(_exchange, "topic", true, false, null);

                        var dictionary = new Dictionary<string, object>();

                        if (_isPriority)
                        {
                            dictionary.Add("x-queue-type", "classic");
                            dictionary.Add("x-max-priority", 10);
                            _model.QueueDeclare(_queue, true, false, false, dictionary);
                        }
                        else
                            _model.QueueDeclare(_queue, true, false, false, dictionary);

                        foreach (var routingKey in _routingKey.Split('|'))
                            _model.QueueBind(_queue, _exchange, routingKey, null);

                        _model.BasicQos(0, _prefetchCount, false);
                        _consumer = new EventingBasicConsumer(_model);
                        _consumer.Received += HandleMessageWithManualAck;

                        _model.BasicConsume(_queue, false, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to establish connection to RabbitMQ!: {@exception}", e);
                throw;
            }
        }

        public void ConnectWithAutoAck()
        {
            _hostName = _configuration["MessageBroker:Host"];
            _hostPort = _configuration.GetValue<int>("MessageBroker:Port", 5672);
            _userName = _configuration["MessageBroker:Username"];
            _password = _configuration["MessageBroker:Password"];
            _virtualHost = _configuration["MessageBroker:VirtualHost"];
            _exchange = _configuration["MessageBroker:Exchange"];
            _queue = _configuration["MessageBroker:Queue"];
            _consumerName = _configuration["MessageBroker:Consumer"];
            _routingKey = _configuration["MessageBroker:RoutingKey"];
            _prefetchCount = _configuration.GetValue<ushort>("MessageBroker:PrefetchCount", 1000);

            var clientProperties = new Dictionary<string, object>
            {
                { "host_name", Environment.MachineName },
                { "application_name", "Song88 Platform" }
            };

            try
            {
                if (_connection == null)
                {
                    if (_connectionFactory == null)
                        _logger.LogInformation("Connecting to RabbitMQ on [{host}:{port}; TlsEnabled: {tlsEnabled}; AutoAck",
                                _hostName, _hostPort, _hostPort == 5671 ? true : false);

                    _connectionFactory = new ConnectionFactory
                    {
                        HostName = _hostName,
                        Port = _hostPort,
                        UserName = _userName,
                        Password = _password,
                        VirtualHost = _virtualHost,
                        RequestedHeartbeat = 60,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                        Ssl = new SslOption { Enabled = _hostPort == 5671 ? true : false, ServerName = _hostName },
                        ClientProperties = clientProperties
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

                        _model.BasicQos(0, _prefetchCount, false);
                        _consumer = new EventingBasicConsumer(_model);
                        _consumer.Received += HandleMessageWithManualAck;

                        _model.BasicConsume(_queue, true, $"{_consumerName}.{Guid.NewGuid()}", _consumer);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to establish connection to RabbitMQ!: {@exception}", e);
                throw;
            }
        }

        private void HandleMessageWithManualAck(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                MessageReceived(sender, e);
            }
            catch (Exception)
            {
                _consumer.Model.BasicNack(e.DeliveryTag, false, true);
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
            Publish(payload, messageType, routingKey, correlationId, 0);
        }

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, int priority)
        {
            using (var activity = _activitySource.StartActivity($"{routingKey} send", ActivityKind.Producer))
            {
                var properties = _model.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.DeliveryMode = 2;
                properties.Type = messageType;
                if (priority != 0)
                    properties.Priority = Convert.ToByte(priority);

                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);

                var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

                _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
            }
        }

        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            Publish(payload, messageType, routingKey, correlationId, 0);
        }

        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId, int priority)
        {
            using (var activity = _activitySource.StartActivity($"{routingKey} send", ActivityKind.Producer))
            {
                var conn = GetConnection();
                using var channel = conn.CreateModel();

                var properties = channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.DeliveryMode = 2;
                properties.Type = messageType;
                if (priority != 0)
                    properties.Priority = Convert.ToByte(priority);

                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);

                var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

                channel.BasicPublish(_exchange, routingKey, properties, payloadBytes);
            }
        }

        public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            var properties = _externalModel.CreateBasicProperties();
            properties.CorrelationId = correlationId;
            properties.DeliveryMode = 2;
            properties.Type = messageType;

            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            _externalModel.BasicPublish(_externalExchange, routingKey, properties, payloadBytes);
        }

        public void Publish(string payload, string messageType, string routingKey, string correlationId)
        {
            using (var activity = _activitySource.StartActivity($"{routingKey} send", ActivityKind.Producer))
            {
                var properties = _model.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.DeliveryMode = 2;
                properties.Type = messageType;

                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);


                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                _model.BasicPublish(_exchange, routingKey, properties, payloadBytes);
            }
        }

        public void CPublish(string payload, string messageType, string routingKey, string correlationId)
        {
            using (var activity = _activitySource.StartActivity($"{routingKey} send", ActivityKind.Producer))
            {
                var conn = GetConnection();
                using var channel = conn.CreateModel();

                var properties = channel.CreateBasicProperties();
                properties.CorrelationId = correlationId;
                properties.DeliveryMode = 2;
                properties.Type = messageType;

                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);


                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                channel.BasicPublish(_exchange, routingKey, properties, payloadBytes);
            }
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            try
            {
                if (props.Headers == null)
                    props.Headers = new Dictionary<string, object>();
                props.Headers[key] = value;
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            if (_model != null)
                _model.Dispose();
            if (_connection != null)
                _connection.Dispose();
        }

        private IConnection GetConnection()
        {
            if (_connection != null && _connection.IsOpen)
                return _connection;

            lock (_lock)
            {
                Connect(); //
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection = _connectionFactory.CreateConnection();
                }
            }
            return _connection;
        }
    }
}

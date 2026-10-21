using System;
using System.Collections.Generic;
using System.Threading;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.Job.Resettlement.Test
{
    internal sealed record PublishedMessage(object Payload, string MessageType, string RoutingKey, string CorrelationId);

    internal sealed class FakeMessageBroker : IMessageBroker
    {
        private readonly object _lock = new object();
        private readonly List<PublishedMessage> _published = new List<PublishedMessage>();
        private int _disposed;

        // Runs after each Publish<T> is recorded, for example to cancel a token in the middle of a batch.
        public Action OnPublish { get; set; }

        public bool Disposed => Volatile.Read(ref _disposed) == 1;

        public PublishedMessage[] Published
        {
            get { lock (_lock) return _published.ToArray(); }
        }

        public event EventHandler<BasicDeliverEventArgs> MessageReceived
        {
            add { }
            remove { }
        }

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            lock (_lock) _published.Add(new PublishedMessage(payload, messageType, routingKey, correlationId));
            OnPublish?.Invoke();
        }

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

        public void Connect() { }
        public void ExternalConnect() { }
        public void ConnectWithAutoAck() { }
        public void ConnectWithManualAck() { }
        public void Acknowledge(BasicDeliverEventArgs e) { }
        public void NAcknowledge(BasicDeliverEventArgs e) { }
        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) { }
        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId) { }
        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) { }
        public void Publish(string payload, string messageType, string routingKey, string correlationId) { }
        public void CPublish(string payload, string messageType, string routingKey, string correlationId) { }
        public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId) { }
    }
}

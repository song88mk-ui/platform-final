using Platform.Shared.Utility;
using RabbitMQ.Client.Events;

namespace Platform.Operator.Servicev2.Test.Repository
{
    // Captures what BettingRepository publishes. Only the two Publish<T> overloads the cashout
    // paths use are implemented; anything else throwing keeps an accidental new dependency visible.
    // Same shape as CacheSync.Service.Test's FakeMessageBroker.
    public class FakeMessageBroker : IMessageBroker
    {
        public List<PublishedMessage> Published { get; } = new List<PublishedMessage>();

        public event EventHandler<BasicDeliverEventArgs> MessageReceived;

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId)
        {
            Published.Add(new PublishedMessage { Payload = payload, MessageType = messageType, RoutingKey = routingKey, CorrelationId = correlationId });
        }

        public void Publish<T>(T payload, string messageType, string routingKey, string correlationId, int priority)
        {
            Published.Add(new PublishedMessage { Payload = payload, MessageType = messageType, RoutingKey = routingKey, CorrelationId = correlationId });
        }

        public void Connect() => throw new NotImplementedException();
        public void ExternalConnect() => throw new NotImplementedException();
        public void ConnectWithAutoAck() => throw new NotImplementedException();
        public void ConnectWithManualAck() => throw new NotImplementedException();
        public void Acknowledge(BasicDeliverEventArgs e) => throw new NotImplementedException();
        public void NAcknowledge(BasicDeliverEventArgs e) => throw new NotImplementedException();
        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId) => throw new NotImplementedException();
        public void CPublish<T>(T payload, string messageType, string routingKey, string correlationId, int priority) => throw new NotImplementedException();
        public void Publish(string payload, string messageType, string routingKey, string correlationId) => throw new NotImplementedException();
        public void CPublish(string payload, string messageType, string routingKey, string correlationId) => throw new NotImplementedException();
        public void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId) => throw new NotImplementedException();
        public void Dispose() { }

        protected virtual void OnMessageReceived(BasicDeliverEventArgs e) => MessageReceived?.Invoke(this, e);
    }

    public class PublishedMessage
    {
        public object Payload { get; set; }
        public string MessageType { get; set; }
        public string RoutingKey { get; set; }
        public string CorrelationId { get; set; }
    }
}

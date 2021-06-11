using RabbitMQ.Client.Events;
using System;

namespace Platform.Shared.Utility
{
    public interface IMessageBroker
    {
        event EventHandler<BasicDeliverEventArgs> MessageReceived;
        void Connect();
        void ExternalConnect();
        void ConnectWithManualAck();
        void Acknowledge(BasicDeliverEventArgs e);
        void NAcknowledge(BasicDeliverEventArgs e);
        void Publish<T>(T payload, string messageType, string routingKey, string correlationId);
        void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId);
        void Publish(string payload, string messageType, string routingKey, string correlationId);
        void Dispose();
    }
}

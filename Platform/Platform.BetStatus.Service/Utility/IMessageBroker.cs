using RabbitMQ.Client.Events;
using System;

namespace Platform.BetStatus.Service.Utility
{
    public interface IMessageBroker
    {
        event EventHandler<BasicDeliverEventArgs> MessageReceived;

        void ExternalConnect();
        void Connect();
        void ExternalPublish<T>(T payload, string messageType, string routingKey, string correlationId);
        void Publish<T>(T payload, string messageType, string routingKey, string correlationId);
        void Publish(string payload, string messageType, string routingKey, string correlationId);

        void Dispose();
    }
}

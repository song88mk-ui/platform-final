using RabbitMQ.Client.Events;
using System;

namespace Platform.Betting.API.Utility
{
    public interface IMessageBroker
    {
        event EventHandler<BasicDeliverEventArgs> MessageReceived;
        void Connect();
        void Publish<T>(T payload, string messageType, string routingKey, string correlationId);
        void Publish(string payload, string messageType, string routingKey, string correlationId);

        void Dispose();
    }
}

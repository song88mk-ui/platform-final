using Microsoft.AspNetCore.SignalR.Client;
using Platform.WebSocket.API.Messages;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.WebSocket.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/platform")                    
                .WithAutomaticReconnect()
                .Build();

            await connection.StartAsync();

            await connection.InvokeAsync("SubscribeToEndpoint", new { ClientId = "5093617A-9367-EB11-89FB-00224815B68B", Endpoint = "test" });
            connection.On<OutgoingOddsChange>("OnOddsChange", ProcessOddsChange);

            Console.ReadLine();
        }

        private static void ProcessOddsChange(OutgoingOddsChange message)
        {
            Console.WriteLine($"{DateTime.UtcNow} : {JsonSerializer.Serialize(message)}");
        }
    }
}

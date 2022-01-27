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
                .WithUrl("http://localhost:61801/platform")                    
                .WithAutomaticReconnect()
                .Build();

            connection.On<OutgoingOddsChange>("OnOddsChange", ProcessOddsChange);
            await connection.StartAsync();

            Console.ReadLine();
        }

        private static void ProcessOddsChange(OutgoingOddsChange message)
        {
            Console.WriteLine($"{DateTime.UtcNow} : {JsonSerializer.Serialize(message)}");
        }
    }
}

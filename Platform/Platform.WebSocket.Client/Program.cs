using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
                .WithUrl("https://ws.song88.com/platform")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect()
                .Build();

            await connection.StartAsync();

            await connection.InvokeAsync("SubscribeToBetStatus", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });
            await connection.InvokeAsync("UnsubscribeToBetStatus", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });

            await connection.InvokeAsync("SubscribeToCashOutChange", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });
            await connection.InvokeAsync("UnsubscribeToCashOutChange", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });

            await connection.InvokeAsync("SubscribeToWalletChange", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });
            await connection.InvokeAsync("UnsubscribeToWalletChange", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });

            await connection.InvokeAsync("SubscribeToFavorites", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });
            await connection.InvokeAsync("UnsubscribeToFavorites", new { ClientId = "71BBE7D8-FD04-459C-6D8B-08D9354E5B64", UserId = "user123" });

            Console.ReadLine();
        }
        private static void ProcessOddsChange(OutgoingOddsChange message)
        {
            Console.WriteLine($"{DateTime.UtcNow} : {JsonSerializer.Serialize(message)}");
        }
    }
}

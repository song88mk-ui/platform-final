using Microsoft.Extensions.Hosting;
using Platform.CacheSync.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static App.Metrics.Health.HealthCheck;

namespace Platform.CacheSync.Service
{
    public class Worker : BackgroundService
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ICacheSyncRepository _cacheSyncRepository;

        public Worker(IMessageBroker messageBroker, ICacheSyncRepository cacheSyncRepository)
        {
            _messageBroker = messageBroker;
            _cacheSyncRepository = cacheSyncRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBroker.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _messageBroker.Connect();
            _messageBroker.MessageReceived += MessageReceivedEvent;

            return Task.CompletedTask;
        }

        private void MessageReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Body);
            var messageType = e.BasicProperties.Type;
            try
            {
                Console.WriteLine(
                    $"CacheSync Start {DateTime.UtcNow} {messageType} -> {payload}");

                switch (messageType)
                {
                    case "OnMarketSortOrderChange":
                        var marketSortOrderMessage = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessMarketSortOrderChange(marketSortOrderMessage).GetAwaiter().GetResult();
                        break;
                    case "OnSportSortOrderChange":
                        var sportSortOrderMessage = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessSportSortOrderChange(sportSortOrderMessage).GetAwaiter().GetResult();
                        break;
                    case "OnUpdateUserFavorites":
                        var userFavorite = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessUpdateUserFavorites(userFavorite).GetAwaiter().GetResult();
                        break;
                    case "OnMaintenance":
                        var maintenance = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessUpdateUserFavorites(maintenance).GetAwaiter().GetResult();
                        break;
                    case "OnParlayBonusChange":
                        var parlayBonus = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessParlayBonusChange(parlayBonus).GetAwaiter().GetResult();
                        break;
                    case "OnClientChange":
                        //
                        var client = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessClientChange(client).GetAwaiter().GetResult();
                        break;
                    case "OnDynamicTranslationChange":
                        var translationRequest = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessDynamicTranslationChange(translationRequest).GetAwaiter().GetResult();
                        break;
                    case "OnTranslationChange":
                        var translationChange = JsonSerializer.Deserialize<OnTranslationChangeMessage>(payload);
                        _cacheSyncRepository.ProcessTranslationChange(translationChange).GetAwaiter().GetResult();
                        break;
                    case "OnStaticTranslationChange":
                        var staticTranslation = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessStaticTranslationChange(staticTranslation).GetAwaiter().GetResult();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{messageType} {payload}");
                Console.WriteLine($"{ex.Message} - {ex.StackTrace}");
            }
        }
    }
}

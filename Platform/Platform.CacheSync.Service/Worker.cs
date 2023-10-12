using Microsoft.Extensions.Hosting;
using Platform.CacheSync.Service.Repository;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service
{
    public class Worker : BackgroundService
    {
        private readonly IMessageBroker _messageBroker;
        private readonly ICacheSyncRepository _cacheSyncRepository;

        public Worker(IMessageBroker messageBroker, ICacheSyncRepository cacheSyncRepository)
        {
            //
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
                switch (messageType)
                {
                    case "OnMarketSortOrderChange":
                        var marketSortOrderMessage = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessMarketSortOrderChange(marketSortOrderMessage);
                        break;
                    case "OnSportSortOrderChange":
                        var sportSortOrderMessage = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessSportSortOrderChange(sportSortOrderMessage);
                        break;
                    case "OnUpdateUserFavorites":
                        var userFavorite = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessUpdateUserFavorites(userFavorite);
                        break;
                    case "OnMaintenance":
                        var maintenance = JsonSerializer.Deserialize<OnDeleteCacheMessage>(payload);
                        _cacheSyncRepository.ProcessUpdateUserFavorites(userFavorite);
                        break;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}

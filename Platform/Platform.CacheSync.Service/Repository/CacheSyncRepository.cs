using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Repository
{
    public class CacheSyncRepository : ICacheSyncRepository
    {
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IMessageBroker _messageBroker;
        private readonly IConfiguration _configuration;

        public CacheSyncRepository(ISportsbookRepository sportsbookRepository, IMessageBroker messageBroker, IConfiguration configuration)
        {
            _sportsbookRepository = sportsbookRepository;
            _messageBroker = messageBroker;
            _configuration = configuration;
        }

        public async void ProcessMarketSortOrderChange(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.MarketSortOrder
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);

            if (publishMessage)
            {
                _messageBroker.Publish(true, "OnMarketSortOrderChange", $"r.oe.sb.engine.onmarketsortorderchange.{message.ClientCode.ToLower()}", Guid.NewGuid().ToString().ToUpper());
            }
        }

        public async void ProcessSportSortOrderChange(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.SportSortOrder
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);
        }

        public async void ProcessUpdateUserFavorites(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.UserFavorites,
                CacheKey = message.CacheKey                
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);
        }

        public async void ProcessMaintenance(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.Maintenance
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);
        }


        public async void ProcessParlayBonusChange(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = message.CacheType            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);
        }

        #region Private Methods
        private async Task<bool> DeleteCachePreload_SportsbookApi(CachePreloadRequestDTO request)
        {
            var apis = bool.Parse(_configuration["IsProduction"]) ? _configuration.GetSection("API:Production:PlatformSportsbookAPI").Get<List<string>>() : _configuration.GetSection("API:Staging:PlatformSportsbookAPI").Get<List<string>>();
            var publishMessage = true;

            foreach (var api in apis)
            {
                var result = await _sportsbookRepository.DeleteCachePreload(request, api);

                if (result.StatusCode != StatusCode.Success)
                {
                    publishMessage = false;
                    break;
                }
            }

            return publishMessage;
        }

        private async Task<bool> DeleteCachePreload_BettingApi(CachePreloadRequestDTO request)
        {
            var apis = bool.Parse(_configuration["IsProduction"]) ? _configuration.GetSection("API:Production:PlatformBettingAPI").Get<List<string>>() : _configuration.GetSection("API:Staging:PlatformBettingAPI").Get<List<string>>();
            var publishMessage = true;

            foreach (var api in apis)
            {
                var result = await _sportsbookRepository.DeleteCachePreload(request, api);

                if (result.StatusCode != StatusCode.Success)
                {
                    publishMessage = false;
                    break;
                }
            }

            return publishMessage;
        }


        private async Task<bool> DeleteCachePreload_PlatformApi(CachePreloadRequestDTO request)
        {
            var apis = bool.Parse(_configuration["IsProduction"]) ? _configuration.GetSection("API:Production:PlatformAPI").Get<List<string>>() : _configuration.GetSection("API:Staging:PlatformAPI").Get<List<string>>();
            var publishMessage = true;

            foreach (var api in apis)
            {
                var result = await _sportsbookRepository.DeleteCachePreload(request, api);

                if (result.StatusCode != StatusCode.Success)
                {
                    publishMessage = false;
                    break;
                }
            }

            return publishMessage;
        }
        #endregion
    }
}

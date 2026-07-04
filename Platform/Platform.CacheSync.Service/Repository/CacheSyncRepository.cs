using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Repository
{
    public class CacheSyncRepository : ICacheSyncRepository
    {
        private readonly IHelper _helper;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IMessageBroker _messageBroker;
        private readonly IConfiguration _configuration;

        public CacheSyncRepository(IHelper helper, ISportsbookRepository sportsbookRepository, IMessageBroker messageBroker, IConfiguration configuration)
        {
            _helper = helper;
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
                CacheType = message.CacheType
            };

            await DeleteCachePreload_BettingApi(request);
        }

        public async void ProcessClientChange(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = message.CacheType,
                CacheKey = message.CacheKey
            };

            await DeleteCachePreload_PlatformApi(request);
        }

        public async void ProcessDynamicTranslationChange(OnDeleteCacheMessage message)
        {
            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.DynamicTranslation,
                // The legacy ON_DYNAMIC_TRANSLATION_CHANGE message is purge-only (no keys/language
                // on the wire — matching the hardcoded PurgeAll=true re-publish to the adapters
                // below). Without this flag the request reached Sportsbook.API with neither keys
                // nor PurgeAll, and InvalidateTranslationsCache's empty-keys guard silently
                // no-opped — the sb:translation cache was never purged on this path.
                PurgeAll = true
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);

            if (publishMessage)
            {
                for (int i = 0; i < 10; i++)
                {
                    _messageBroker.Publish(
                        new OnDeleteCacheMessage { CacheType = CacheType.DynamicTranslation, PurgeAll = true },
                        "OnDynamicTranslationChange",
                        $"r.oe.adapter.ondynamictranslationchange.{i:D2}",
                        Guid.NewGuid().ToString().ToUpper());
                }
            }
        }

        // Payload-carrying successor to ProcessDynamicTranslationChange (2026-07 translation-flow
        // redesign): purges the Sportsbook.API translation cache PER CHANGED KEY (the deltas carry
        // exactly the (StringId, TargetLanguage) pairs to invalidate — no purge-all), then relays
        // the changed TranslationCache2 rows on r.oe.ontranslationchange, where the topic exchange
        // delivers them to both OE.Adapter.Service (in-memory enrichment cache) and
        // OE.Translation.Service (Mongo persistence + translation-only odds pushes).
        public async void ProcessTranslationChange(OnTranslationChangeMessage message)
        {
            if (message?.Tr == null || message.Tr.Count == 0)
                return;

            var request = new CachePreloadRequestDTO
            {
                CacheType = CacheType.DynamicTranslation,
                Translations = message.Tr
                    .Where(t => t != null
                        && !string.IsNullOrWhiteSpace(t.StId)
                        && !string.IsNullOrWhiteSpace(t.LaCo))
                    .Select(t => new TranslationChangeRequestDTO
                    {
                        StringId = t.StId,
                        TargetLanguage = t.LaCo,
                        TargetText = t.Tx
                    })
                    .ToList()
            };

            var publishMessage = await DeleteCachePreload_SportsbookApi(request);

            if (publishMessage)
            {
                _messageBroker.Publish(
                    message,
                    "OnTranslationChange",
                    "r.oe.ontranslationchange",
                    Guid.NewGuid().ToString().ToUpper());
            }
        }

        #region Private Methods
        private async Task<bool> DeleteCachePreload_SportsbookApi(CachePreloadRequestDTO request)
        {
            var hostName = _configuration.GetSection("API:PlatformSportsbookAPI:Host").Get<string>();
            var port = _configuration.GetSection("API:PlatformSportsbookAPI:Port").Get<string>();
            var publishMessage = true;

            var addresses = await Dns.GetHostAddressesAsync(hostName);

            foreach (var address in addresses)
            {
                try
                {
                    var url = $"http://{address}:{port}";

                    var result = await _sportsbookRepository.DeleteCachePreload(request, url);

                    Console.WriteLine(
                        $"Purged {url} -> {request.CacheType.ToString()} - {(int)result.StatusCode} {result.Message}");

                    if (result.StatusCode != StatusCode.Success)
                    {
                        publishMessage = false;
                        break;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to purge {address}: {ex.Message}");
                }
            }

            return publishMessage;
        }

        private async Task<bool> DeleteCachePreload_BettingApi(CachePreloadRequestDTO request)
        {
            var hostName = _configuration.GetSection("API:PlatformBettingAPI:Host").Get<string>();
            var port = _configuration.GetSection("API:PlatformSportsbookAPI:Port").Get<string>();
            var publishMessage = true;

            var addresses = await Dns.GetHostAddressesAsync(hostName);

            foreach (var address in addresses)
            {
                try
                {
                    var url = $"http://{address}:{port}";

                    var result = await _sportsbookRepository.DeleteCachePreload(request, url);

                    Console.WriteLine(
                        $"Purged {url} -> {request.CacheType.ToString()} - {(int)result.StatusCode} {result.Message}");

                    if (result.StatusCode != StatusCode.Success)
                    {
                        publishMessage = false;
                        break;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to purge {address}: {ex.Message}");
                }
            }


            return publishMessage;
        }


        private async Task<bool> DeleteCachePreload_PlatformApi(CachePreloadRequestDTO request)
        {
            var hostName = _configuration.GetSection("API:PlatformAPI:Host").Get<string>();
            var port = _configuration.GetSection("API:PlatformSportsbookAPI:Port").Get<string>();
            var publishMessage = true;

            var header = new Dictionary<string, string>();
            header["x-api-key"] = _configuration.GetSection("API:PlatformAPI:APIKey").Get<string>();

            var addresses = await Dns.GetHostAddressesAsync(hostName);

            foreach (var address in addresses)
            {
                try
                {
                    var url = $"http://{address}:{port}";

                    var result = await _sportsbookRepository.DeleteCachePreload(request, url, header);

                    Console.WriteLine(
                        $"Purged {url} -> {request.CacheType.ToString()} - {(int)result.StatusCode} {result.Message}");

                    if (result.StatusCode != StatusCode.Success)
                    {
                        publishMessage = false;
                        break;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to purge {address}: {ex.Message}");
                }
            }

            return publishMessage;
        }
        #endregion
    }
}

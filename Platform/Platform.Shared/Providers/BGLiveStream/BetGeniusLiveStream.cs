using Platform.Shared.Enums;
using Platform.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using Platform.Shared.Models.Provider.BetGenius;
using System.Linq;

namespace Platform.Shared.Providers.BGLiveStream
{
    public class BetGeniusLiveStream : ILiveStreamProvider
    {
        public ChannelType ChannelType => ChannelType.BG;

        private readonly IBetGeniusRepository _bgRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BetGeniusLiveStream> _logger;

        public BetGeniusLiveStream(
            IConfiguration configuration,
            ILogger<BetGeniusLiveStream> logger,
            IBetGeniusRepository bgRepo)
        {
            _configuration = configuration;
            _logger = logger;
            _bgRepo = bgRepo;
        }

        public async Task<string> GetLiveStream(string fixtureId, string liveStream, string region, string device)
        {
            try
            {
                _logger.LogInformation($"GetLiveStream Start : {fixtureId} LiveStream:{liveStream}");

                var liveStreamDto = JsonSerializer.Deserialize<BGLiveStreamDto>(liveStream);
                if(!liveStreamDto.Pe.Any(p => p.ToLower() == $"{region.ToLower()}:{device.ToLower()}"))
                    return string.Empty;

                var token = await _bgRepo.GetBetGeniusAuthorizationToken();

                if (token.StatusCode != StatusCode.Success)
                {
                    _logger.LogError($"GetLiveStream: GetBetGeniusAuthorizationToken Failed: {token.Message}-{JsonSerializer.Serialize(token)}");
                    return string.Empty;
                }

                var deliveryId = liveStreamDto.Ot.Hls.FirstOrDefault()?.Id ?? string.Empty;
                var result = await _bgRepo.GetLiveStreamToken(token.Result.IdToken, fixtureId, liveStreamDto.Id, deliveryId, region, device);
                if (result == null)
                {
                    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Failed: null");
                    return string.Empty;
                }

                if (result.StatusCode == StatusCode.Error)
                {
                    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Failed: {result.Message}-{JsonSerializer.Serialize(result)}");
                    return string.Empty;
                }
                else if (result.StatusCode == StatusCode.Livestream_Is_Not_Available_In_Your_Region)
                {
                    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Not Available: {fixtureId}");
                    return string.Empty;
                }

                _logger.LogInformation($"GetLiveStream: GetBetGeniusFixture Result : {JsonSerializer.Serialize(result)}");

                var response = result.Result;
                if (response?.Token == null)
                {
                    _logger.LogInformation("GetLiveStream: No LiveStreams available");
                    return string.Empty;
                }

                _logger.LogInformation($"GetLiveStream Result for {fixtureId}: {JsonSerializer.Serialize(response)}");

                return response?.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetLiveStream Error: {ex.Message}-{ex.StackTrace}");
                return string.Empty;
            }
        }
    }
}

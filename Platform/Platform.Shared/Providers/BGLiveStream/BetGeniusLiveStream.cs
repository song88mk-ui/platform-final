using Platform.Shared.Enums;
using Platform.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using Platform.Shared.Models.Provider.BetGenius;

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
                _logger.LogInformation($"GetLiveStream Start : {fixtureId}");

                var token = await _bgRepo.GetBetGeniusAuthorizationToken();

                if (token.StatusCode != StatusCode.Success)
                {
                    _logger.LogError($"GetLiveStream: GetBetGeniusAuthorizationToken Failed: {token.Message}-{JsonSerializer.Serialize(token)}");
                    return string.Empty;
                }
                
                var liveStreamDto = JsonSerializer.Deserialize<BGLiveStreamDto>(liveStream);
                //TODO
                //var result = await _bgRepo.GetLiveStream(token.Result.IdToken, fixtureId, liveStreamDto.Id, liveStreamDto.Ot, region, device);
                //if (result == null)
                //{
                //    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Failed: null");
                //    return (false, string.Empty);
                //}

                //if (result.StatusCode == StatusCode.Error)
                //{
                //    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Failed: {result.Message}-{JsonSerializer.Serialize(result)}");
                //    return (false, string.Empty);
                //}
                //else if (result.StatusCode == StatusCode.Bet_Not_Found)
                //{
                //    _logger.LogError($"GetLiveStream: GetBetGeniusFixture Not Found: {fixtureId}");
                //    return (false, string.Empty);
                //}

                //_logger.LogInformation($"GetLiveStream: GetBetGeniusFixture Result : {JsonSerializer.Serialize(result)}");
                
                //var fixture = result.Result;
                //if (fixture?.LiveStreams == null || !fixture.LiveStreams.Any())
                //{
                //    _logger.LogInformation("GetLiveStream: No LiveStreams available");
                //    return (false, string.Empty);
                //}
                    
                //var liveStreams = fixture.LiveStreams.Select(s => 
                //    new { 
                //        Id = s.Id, 
                //        Pe = s.Permissions.Select(s => $"{s.Device}:{s.Region}").ToList(), 
                //        Ot = s.Deliveries
                //    });

                _logger.LogInformation($"GetLiveStream Result for {fixtureId}: {liveStream}");

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetLiveStream Error: {ex.Message}-{ex.StackTrace}");
                return string.Empty;
            }
        }
    }
}

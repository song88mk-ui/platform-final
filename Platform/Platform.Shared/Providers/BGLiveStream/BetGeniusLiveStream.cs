using Platform.Shared.Enums;
using Platform.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;
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

        public async Task<bool> ValidateLiveStream(string fixtureId, string countryCode, string device)
        {
            try
            {
                _logger.LogInformation($"ValidateLiveStream Start : {fixtureId}-{countryCode}-{device}");

                var token = await _bgRepo.GetBetGeniusAuthorizationToken();

                if (token.StatusCode != StatusCode.Success)
                {
                    _logger.LogError($"ValidateLiveStream: GetBetGeniusAuthorizationToken Failed: {token.Message}-{JsonSerializer.Serialize(token)}");
                    return false;
                }

                var result = await _bgRepo.GetBetGeniusFixture(token.Result.IdToken, fixtureId);
                if (result == null)
                {
                    _logger.LogError($"ValidateLiveStream: GetBetGeniusFixture Failed: null");
                    return false;
                }

                if (result.StatusCode == StatusCode.Error)
                {
                    _logger.LogError($"ValidateLiveStream: GetBetGeniusFixture Failed: {result.Message}-{JsonSerializer.Serialize(result)}");
                    return false;
                }

                _logger.LogInformation($"ValidateLiveStream: GetBetGeniusFixture Result : {JsonSerializer.Serialize(result)}");
                
                var fixture = result.Result;
                if (fixture?.LiveStreams == null || !fixture.LiveStreams.Any())
                {
                    _logger.LogInformation("ValidateLiveStream: No LiveStreams available");
                    return false;
                }

                var normalizedDevice = device?.ToUpperInvariant();
                var normalizedCountry = countryCode?.ToUpperInvariant();

                var isAllowed = fixture.LiveStreams.Any(ls =>
                    ls.Permissions != null &&
                    ls.Permissions.Any(p =>
                        string.Equals(p.Device, normalizedDevice, StringComparison.OrdinalIgnoreCase)
                            && (string.IsNullOrEmpty(p.Region) || string.Equals(p.Region, normalizedCountry, StringComparison.OrdinalIgnoreCase))
                    )
                );

                _logger.LogInformation(
                    $"ValidateLiveStream Result : {isAllowed} for {fixtureId}-{countryCode}-{device}");

                return isAllowed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessCancellation Error: {ex.Message}-{ex.StackTrace}");
            }

            return true;
        }
    }
}

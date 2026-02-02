using Platform.Shared.Enums;
using Platform.Shared.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

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
                    return false;

                var result = await _bgRepo.GetBetGeniusFixture(token.Result.IdToken, fixtureId);
                if(result == null) return false;
                if(result.StatusCode == StatusCode.Error) return false;

                //TODO: logic for live

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessCancellation Error: {ex.Message}-{ex.StackTrace}");
            }

            return true;
        }
    }
}

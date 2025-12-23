using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Extensions;
using Platform.Shared.Repositories;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Platform.Shared.Providers.GTS
{
    public class GTSBetProvider : IBetProvider
    {
        public ChannelType ChannelType => ChannelType.BG;

        private readonly IGTSAuthRepository _auth;
        private readonly IGTSRepository _gtsRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GTSBetProvider> _logger;

        public GTSBetProvider(
            IConfiguration configuration,
            ILogger<GTSBetProvider> logger, 
            IGTSAuthRepository auth,
            IGTSRepository gtsRepo)
        {
            _configuration = configuration;
            _logger = logger;
            _auth = auth;
            _gtsRepo = gtsRepo;
        }

        public async Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken ct)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Accepted (GTS disabled)"
                };

            if (bet.BetSelections.Any(c => c.BetSelection.SportId.IsVirtualSportId()))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Accepted (GTS disabled for VS)"
                };

            var token = await _auth.GetGTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = "1025 - GTS Authentication Failed"
                };

            var result = await _gtsRepo.Assessment(token.Result.Token, bet);

            if (result.Result.IsBetAllowed == false)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = result.Result.Message
                };

            var sendBet = await _gtsRepo.SendBet(token.Result.Token, bet);

            if(sendBet.StatusCode != StatusCode.Success)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = "1026 - Failed sending Bet to GTS"
                };

            return new BetProviderResult
            {
                Acceptance = result.Result.IsBetAllowed ? BetAcceptance.Accepted : BetAcceptance.Rejected,
                Reason = result.Result.Message ?? string.Empty
            };
        }

        public async Task ProcessCancellation(OnBetTimeout message, string correlationId, bool isBackOfficeTrigger = false)
        {

            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return;

            try
            {

                var token = await _auth.GetGTSAuthorizationToken();
                if (token.StatusCode != StatusCode.Success)
                    return;

                _logger.LogInformation($"ProcessCancellation (Request)- {JsonSerializer.Serialize(message)}");

                var result = await _gtsRepo.Cancellation(token.Result.Token, message);

                _logger.LogInformation($"ProcessCancellation (Response)- {JsonSerializer.Serialize(result)}");

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessCancellation Error- {JsonSerializer.Serialize(ex)}");
            }
        }

        public async Task<bool> IsAvailableAsync(BetRequestDTO request)
        {
            var token = await _auth.GetGTSAuthorizationToken();
            return token.StatusCode == StatusCode.Success;
        }

        public async Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request)
        {
            var token = await _auth.GetGTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return new ProviderStakeResult
                {
                    IsSuccess = false,
                    MaxStake = 0,
                    StatusCode = (int)token.StatusCode,
                    Message = "GTS_AUTH_FAILED"
                };

            var result = await _gtsRepo.GetMaxStake(
                token.Result.Token,
                request);

            return result.StatusCode == StatusCode.Success
                ? new ProviderStakeResult
                {
                    IsSuccess = true,
                    MaxStake = result.Result.Stake,
                    StatusCode = (int)result.StatusCode,
                    Message = result.StatusCode.ToString()
                }
                : new ProviderStakeResult
                {
                    IsSuccess = false,
                    MaxStake = 0,
                    StatusCode = (int)result.StatusCode,
                    Message = !string.IsNullOrEmpty(result.Message) ? result.Message : "GTS_MAXSTAKE_FAILED"
                };
        }
    }
}

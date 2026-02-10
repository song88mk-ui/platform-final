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
using Platform.Shared.Models.Provider.GTS;
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

            if (bet.DelayedBetAcceptance == null && bet.BetSelections.Any(c => c.BetSelection.SportId.IsVirtualSportId()))
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

            var providerResult = new ProviderAssessmentResponseDTO {
                IsBetAllowed = false
            };

            if(bet.DelayedBetAcceptance == null)
            {
                var result = await _gtsRepo.Assessment(token.Result.Token, bet);

                providerResult = result.Result;

                if (providerResult.IsBetAllowed == false)
                    return new BetProviderResult
                    {
                        Acceptance = result.Result.IsPending ? BetAcceptance.Pending : BetAcceptance.Rejected,
                        Reason = result.Result.Message
                    };
            }
            else
            {
                providerResult.IsBetAllowed = bet.DelayedBetAcceptance == BetAcceptance.Accepted;
                providerResult.Message = bet.DelayedMessage;
            }

            if(providerResult.IsBetAllowed)
            {
                var sendBet = await _gtsRepo.SendBet(token.Result.Token, new SendBet()
                {
                    Bet = bet.Bet,
                    BetSelections = bet.BetSelections.Select(s => s.BetSelection).ToList()
                });

                if (sendBet.StatusCode != StatusCode.Success)
                    return new BetProviderResult
                    {
                        Acceptance = BetAcceptance.Rejected,
                        Reason = $"1026 - Failed sending Bet to GTS {sendBet.Message}"
                    };
            }

            return new BetProviderResult
            {
                Acceptance = providerResult.IsBetAllowed ? BetAcceptance.Accepted : BetAcceptance.Rejected,
                Reason = providerResult.Message ?? string.Empty
            };

        }

        public async void ProcessCancellation(Guid betId, string limitId, string correlationId, bool isBackOfficeTrigger = false)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return;

            try
            {

                var token = await _auth.GetGTSAuthorizationToken();
                if (token.StatusCode != StatusCode.Success)
                    return;

                _logger.LogInformation("ProcessCancellation (Request) : {betId}", betId);

                var result = await _gtsRepo.Cancellation(token.Result.Token, betId, limitId);

                _logger.LogInformation($"ProcessCancellation (Response) : {JsonSerializer.Serialize(result)}");

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessCancellation Error: {ex.Message}-{ex.StackTrace}");
            }
        }

        public async void ProcessSettlement(OnBetSettlement bet, string correlationId)
        {

            _logger.LogInformation($"ProcessSettlement start");

            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return;

            if (bet.BetSelections.Any(c => c.SportId.IsVirtualSportId()))
                return;

            _logger.LogInformation($"ProcessSettlement: GetGTSAuthorizationToken");

            var token = await _auth.GetGTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return;

            _logger.LogInformation($"ProcessSettlement: SendBet");

            var sendBet = await _gtsRepo.SendBet(token.Result.Token, new SendBet()
            {
                Bet = bet.Bet,
                BetSelections = bet.BetSelections
            }, true);

            if (sendBet.StatusCode != StatusCode.Success)
            {
                _logger.LogInformation($"ProcessSettlement: SendBet Failed {sendBet.Message}-{sendBet.Result}");
                return;
            }
            //TODO: Send to retry if failed

            return;
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

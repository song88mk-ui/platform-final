using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Platform.Shared.Repositories;

namespace Platform.Shared.Providers.MTS
{
    public class MtsBetProvider  : IBetProvider
    {
        public ChannelType ChannelType => ChannelType.SR;

        private readonly IMTSAuthRepository _auth;
        private readonly IMTSMaxStakeRepository _maxStake;

        public MtsBetProvider(
            IMTSAuthRepository auth,
            IMTSMaxStakeRepository maxStake)
        {
            _auth = auth;
            _maxStake = maxStake;
        }

        public async Task<bool> IsAvailableAsync(BetRequestDTO request)
        {
            var token = await _auth.GetMTSAuthorizationToken();
            return token.StatusCode == StatusCode.Success;
        }

        public async Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request)
        {
            var token = await _auth.GetMTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return new ProviderStakeResult
                {
                  IsSuccess = false,
                  MaxStake = 0,
                  StatusCode = (int)token.StatusCode,
                  Message = "MTS_AUTH_FAILED"
                };


            var mtsConfigSelectionRequest = new List<MTSConfigSelectionRequestDTO>();
            foreach (var betSelection in request.Selections)
            {
                mtsConfigSelectionRequest.Add(new MTSConfigSelectionRequestDTO
                {
                    FixtureSpecifier = betSelection.FixtureSpecifier,
                    SelectionSpecifier = betSelection.SelectionSpecifier,
                    Odds = betSelection.DecimalOdds
                });
            }

            var mtsConfigRequest = new MTSConfigRequestDTO
            {
                UserId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                Currency = request.CurrencyCode.ToString(),
                Language = request.LanguageCode.ToString(),
                DeviceId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                LimitId = Convert.ToInt32(request.LimitId),
                BetSelections = mtsConfigSelectionRequest
            };

            var result = await _maxStake.GetMTSMaxStake(
                token.Result.Token,
                mtsConfigRequest,
                request.CompanyShare,
                request.IsBetBuilder,
                request.Odds);

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
                        Message = !string.IsNullOrEmpty(result.Message) ? result.Message : "MTS_MAXSTAKE_FAILED"
                    };
        }
    }
}

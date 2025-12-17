using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Repositories;
using System.Threading.Tasks;

namespace Platform.Shared.Providers.GTS
{
    public class GTSBetProvider : IBetProvider
    {
        public ChannelType ChannelType => ChannelType.SR;

        private readonly IGTSAuthRepository _auth;
        private readonly IGTSRepository _maxStake;

        public GTSBetProvider(
            IGTSAuthRepository auth,
            IGTSRepository maxStake)
        {
            _auth = auth;
            _maxStake = maxStake;
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

            var result = await _maxStake.GetMaxStake(
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

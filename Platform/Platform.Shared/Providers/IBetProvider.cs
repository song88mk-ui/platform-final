using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Shared.Providers
{
    public interface IBetProvider
    {
        ChannelType ChannelType { get; }

        Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request);

        Task<bool> IsAvailableAsync(BetRequestDTO request);
    }

    public class ProviderStakeResult
    {
        public bool IsSuccess { get; set; }
        public decimal MaxStake { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
    }
}

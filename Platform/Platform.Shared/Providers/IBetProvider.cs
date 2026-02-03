using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Shared.Providers
{
    public interface IBetProvider
    {
        ChannelType ChannelType { get; }

        Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken cancellationToken);

        void ProcessCancellation(Guid betId, string correlationId, bool isBackOfficeTrigger = false);
        void ProcessSettlement(OnBetSettlement bet, string correlationId);

        Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request);

        Task<bool> IsAvailableAsync(BetRequestDTO request);
    }

    public sealed class BetProviderResult
    {
        public BetAcceptance Acceptance { get; set; }
        public string Reason { get; set; }
    }

    public class ProviderStakeResult
    {
        public bool IsSuccess { get; set; }
        public decimal MaxStake { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
    }

    public enum BetAcceptance
    {
        Accepted,
        Rejected,
        Pending
    }

    public static class AcceptanceMapper
    {
        public static BetAcceptance ToBetAcceptance(this TicketAcceptance acceptance)
            => acceptance switch
            {
                TicketAcceptance.Accepted => BetAcceptance.Accepted,
                TicketAcceptance.Rejected => BetAcceptance.Rejected,
                _ => throw new ArgumentOutOfRangeException(nameof(acceptance))
            };
    }
}

using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Sportradar.MTS.SDK.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Shared.Providers
{
    public interface IBetProvider
    {
        ChannelType ChannelType { get; }

        Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken cancellationToken);

        // Pre-acceptance cash-out gate: called by Platform.CashOut.Service before any money moves.
        // isFullCashout: full cashout with no prior partial cashouts on the bet.
        Task<BetProviderResult> ValidateCashoutAsync(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount);

        void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger = false);
        // Returns Task (not async void) for the same reason as ProcessCashout below.
        Task ProcessSettlement(OnBetSettlement bet, string correlationId);
        // Returns Task (not async void) so the caller can hold its DI scope open until the
        // provider call completes — otherwise the scoped HttpClient is disposed mid-flight
        // and the update to the provider is silently lost.
        Task ProcessCashout(OnBetSettlement bet, CashoutType cashoutType, string correlationId);

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
        public string Message { get; set; }
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

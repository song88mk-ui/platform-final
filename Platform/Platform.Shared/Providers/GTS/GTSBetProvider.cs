using Platform.Shared.Data.Betting.Entities;
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
using System.Collections.Generic;

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

            var testClientCodes = (_configuration["Custom:TestBetClientCodes"] ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

            string testPrefix = testClientCodes.Contains(bet.Bet.ClientCode, StringComparer.OrdinalIgnoreCase)
                ? "test" : string.Empty;

            var providerResult = new ProviderAssessmentResponseDTO
            {
                IsBetAllowed = false
            };

            if (bet.DelayedBetAcceptance == null)
            {
                var result = await _gtsRepo.Assessment(token.Result.Token, bet, testPrefix);

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

            if (providerResult.IsBetAllowed)
            {

                var sendBet = await _gtsRepo.SendBet(token.Result.Token, new SendBet()
                {
                    Bet = bet.Bet,
                    BetSelections = bet.BetSelections.Select(s => s.BetSelection).ToList()
                }, testPrefix);

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

        public async void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger = false)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return;

            try
            {
                var testClientCodes = (_configuration["Custom:TestBetClientCodes"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
                string testPrefix = testClientCodes.Contains(clientCode, StringComparer.OrdinalIgnoreCase) ? "test" : string.Empty;

                var token = await _auth.GetGTSAuthorizationToken();
                if (token.StatusCode != StatusCode.Success)
                    return;

                _logger.LogInformation("ProcessCancellation (Request) : {betId}", betId);

                var result = await _gtsRepo.Cancellation(token.Result.Token, betId, limitId, clientCode, testPrefix);

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

            var testClientCodes = (_configuration["Custom:TestBetClientCodes"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            string testPrefix = testClientCodes.Contains(bet.Bet.ClientCode, StringComparer.OrdinalIgnoreCase)
                ? "test" : string.Empty;

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
            }, testPrefix, true);

            if (sendBet.StatusCode != StatusCode.Success)
            {
                _logger.LogInformation($"ProcessSettlement: SendBet Failed {sendBet.Message}-{sendBet.Result}");
                return;
            }
            //TODO: Send to retry if failed

            return;
        }

        // Pre-acceptance cash-out eligibility via the GTS Bet Assessment API
        // (GTS Back-End Integration doc): a decline here stops the cashout before any
        // money moves. Read-only on the GTS side. Fail-fast: auth/assessment errors reject.
        public async Task<BetProviderResult> ValidateCashoutAsync(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Cashout Accepted (GTS disabled)"
                };

            if (selections.Any(s => s.SportId.IsVirtualSportId()))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Cashout Accepted (GTS disabled for VS)"
                };

            // Delayed-verdict re-entry: the GTS AssessmentDelayed verdict arrived via Ably
            // (relayed by Platform.Provider.Service) — honor it instead of re-assessing.
            if (message.DelayedBetAcceptance != null)
                return new BetProviderResult
                {
                    Acceptance = message.DelayedBetAcceptance == BetAcceptance.Accepted ? BetAcceptance.Accepted : BetAcceptance.Rejected,
                    Reason = message.DelayedBetAcceptance == BetAcceptance.Accepted
                        ? "1024 - Cashout Accepted (GTS delayed verdict)"
                        : (string.IsNullOrEmpty(message.DelayedMessage) ? "GTS Cashout Assessment Rejected" : message.DelayedMessage)
                };

            var testClientCodes = (_configuration["Custom:TestBetClientCodes"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            string testPrefix = testClientCodes.Contains(bet.ClientCode, StringComparer.OrdinalIgnoreCase)
                ? "test" : string.Empty;

            var token = await _auth.GetGTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = "1025 - GTS Authentication Failed"
                };

            var assessment = await _gtsRepo.CashoutAssessment(token.Result.Token, new OnBetSettlement
            {
                Bet = bet,
                BetSelections = selections
            }, testPrefix, message.HasLive, message.Probabilities);

            if (assessment.StatusCode != StatusCode.Success)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = $"1027 - GTS Cashout Assessment Failed ({assessment.Message})"
                };

            // In-play markets with a configured GTS bet delay answer "AssessmentDelayed" —
            // not a rejection: the real verdict arrives via Ably a few seconds later.
            // Return Pending so CashOut.Service parks the cashout until the verdict.
            if (assessment.Result.IsPending)
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Pending,
                    Reason = "GTS Cashout Assessment Delayed"
                };

            return assessment.Result.IsBetAllowed
                ? new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Cashout Accepted (GTS)"
                }
                : new BetProviderResult
                {
                    Acceptance = BetAcceptance.Rejected,
                    Reason = string.IsNullOrEmpty(assessment.Result.Message) ? "GTS Cashout Assessment Rejected" : assessment.Result.Message
                };
        }

        public async Task ProcessCashout(OnBetSettlement bet, CashoutType cashoutType, string correlationId)
        {
            try
            {
                _logger.LogInformation("ProcessCashout start: {betId} ({cashoutType})", bet.Bet?.BetId, cashoutType);

                if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                    return;

                if (bet.BetSelections.Any(c => c.SportId.IsVirtualSportId()))
                    return;

                // GTS requires cumulative cashedOutTotalStake/cashedOutAmount across all cashouts
                // of the bet. Messages published before the enrichment (still in flight during a
                // deploy) lack them; sending non-cumulative figures would corrupt GTS's view, so
                // those are skipped exactly as before.
                if (cashoutType == CashoutType.Partial && bet.CashedOutTotalTrueStake == null)
                {
                    _logger.LogWarning("ProcessCashout: partial cashout for bet {betId} not forwarded to GTS - message lacks the cumulative cashout figures", bet.Bet?.BetId);
                    return;
                }

                if (bet.Bet?.IsPartialCashout == true && bet.CashedOutTotalTrueStake == null)
                {
                    _logger.LogWarning("ProcessCashout: bet {betId} had prior partial cashouts - GTS update skipped (message lacks the cumulative cashout figures)", bet.Bet.BetId);
                    return;
                }

                var testClientCodes = (_configuration["Custom:TestBetClientCodes"] ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                string testPrefix = testClientCodes.Contains(bet.Bet.ClientCode, StringComparer.OrdinalIgnoreCase)
                    ? "test" : string.Empty;

                var token = await _auth.GetGTSAuthorizationToken();
                if (token.StatusCode != StatusCode.Success)
                {
                    _logger.LogError("ProcessCashout: GTS Authentication Failed for bet {betId}", bet.Bet.BetId);
                    return;
                }

                // The cash-out eligibility assessment runs pre-acceptance in Platform.CashOut.Service
                // (ValidateCashoutAsync via the provider resolver) — by the time this message arrives
                // the cashout is approved and settled, so the only remaining step is the bet update.
                // Full cashout is reported as Settled; a partial leaves the bet Running, which maps
                // to an Open update (payout and leg payoutPrice stay null until final settlement).
                var isSettled = cashoutType == CashoutType.Full;

                GtsCashout gtsCashout;
                if (bet.CashedOutTotalTrueStake != null)
                {
                    // Enriched message: GTS wants totals across all cashouts of the bet, while
                    // Bet.TrueStake holds only the remaining stake once partials happened (and
                    // Bet.Payout only the final event's amount) - restore the original stake for
                    // totalStake, and report a fully cashed-out bet's payout as everything paid
                    // across its cashouts (equals cashedOutAmount, as in the pure-full case).
                    bet.Bet.TrueStake = bet.OriginalTrueStake ?? bet.Bet.TrueStake;
                    if (isSettled)
                        bet.Bet.Payout = bet.CashedOutTotalAmount ?? bet.Bet.Payout;

                    gtsCashout = new GtsCashout
                    {
                        CashedOutTotalStake = bet.CashedOutTotalTrueStake.Value,
                        CashedOutAmount = bet.CashedOutTotalAmount ?? 0m
                    };
                }
                else
                {
                    // Pre-enrichment full cashout with no prior partials: the bet row already
                    // carries the cumulative figures. Bet.Payout holds the cashout amount.
                    gtsCashout = new GtsCashout
                    {
                        CashedOutTotalStake = bet.Bet.TrueStake,
                        CashedOutAmount = bet.Bet.Payout
                    };
                }

                var sendBet = await _gtsRepo.SendBet(token.Result.Token, new SendBet()
                {
                    Bet = bet.Bet,
                    BetSelections = bet.BetSelections
                }, testPrefix, isSettled, gtsCashout);

                if (sendBet.StatusCode != StatusCode.Success)
                {
                    _logger.LogError("ProcessCashout: SendBet Failed for bet {betId}: {message}-{result}", bet.Bet.BetId, sendBet.Message, sendBet.Result);
                    return;
                }

                _logger.LogInformation("ProcessCashout: bet {betId} reported to GTS ({cashoutType} cashout)", bet.Bet.BetId, cashoutType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessCashout Error for bet {betId}", bet.Bet?.BetId);
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

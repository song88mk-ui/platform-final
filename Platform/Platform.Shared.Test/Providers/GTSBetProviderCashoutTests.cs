using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Providers;
using Platform.Shared.Providers.GTS;
using Platform.Shared.Repositories;

namespace Platform.Shared.Test.Providers
{
    public class GTSBetProviderCashoutTests
    {
        private sealed class FakeGtsAuthRepository : IGTSAuthRepository
        {
            public BaseResponseDTO<ProviderAuthResponseDTO> Response { get; set; } = new()
            {
                StatusCode = StatusCode.Success,
                Result = new ProviderAuthResponseDTO { Token = "token" }
            };

            public int Calls { get; private set; }

            public Task<BaseResponseDTO<ProviderAuthResponseDTO>> GetGTSAuthorizationToken()
            {
                Calls++;
                return Task.FromResult(Response);
            }
        }

        private sealed class FakeGtsRepository : IGTSRepository
        {
            public BaseResponseDTO<ProviderAssessmentResponseDTO> CashoutAssessmentResponse { get; set; } = new()
            {
                StatusCode = StatusCode.Success,
                Result = new ProviderAssessmentResponseDTO { IsBetAllowed = true }
            };

            public OnBetSettlement? LastCashoutAssessmentRequest { get; private set; }
            public bool? LastIsInPlay { get; private set; }

            public List<CashedOutProbabilities>? LastProbabilities { get; private set; }

            public Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> CashoutAssessment(string authToken, OnBetSettlement request, string testPrefix = "", bool isInPlay = false, List<CashedOutProbabilities> probabilities = null)
            {
                LastCashoutAssessmentRequest = request;
                LastIsInPlay = isInPlay;
                LastProbabilities = probabilities;
                return Task.FromResult(CashoutAssessmentResponse);
            }

            public (SendBet Request, bool IsSettled, GtsCashout Cashout)? LastSendBet { get; private set; }

            public Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request, string testPrefix = "", bool isSettled = false, GtsCashout cashout = null)
            {
                LastSendBet = (request, isSettled, cashout);
                return Task.FromResult(new BaseResponseDTO<string> { StatusCode = StatusCode.Success });
            }

            public Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request) => throw new NotImplementedException();
            public Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request, string testPrefix = "") => throw new NotImplementedException();
            public Task<BaseResponseDTO<string>> Cancellation(string authToken, Guid betId, string limitId, string clientCode, string testPrefix = "") => throw new NotImplementedException();
        }

        private static GTSBetProvider CreateProvider(FakeGtsAuthRepository auth, FakeGtsRepository repo, bool providerEnabled = true)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Custom:IsProviderEnabled"] = providerEnabled.ToString(),
                    ["Custom:TestBetClientCodes"] = "QTE,KRJ"
                })
                .Build();

            return new GTSBetProvider(configuration, NullLogger<GTSBetProvider>.Instance, auth, repo);
        }

        private static (OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections) CreateCashoutInput(bool hasLive = true) =>
        (
            new OnCashoutBetSettlement { BetId = Guid.NewGuid(), HasLive = hasLive },
            new Bet { BetId = Guid.NewGuid(), ClientCode = "S88", UserId = "user-1", LimitId = "700" },
            new List<BetSelection> { new BetSelection { SportId = 1, BetSelectionStatusId = (int)BetStatus.Running, SelectionSpecifier = "sr:market:sel-1", Odds = 1.9m } }
        );

        [Fact]
        public async Task ValidateCashoutAsync_ProviderDisabled_AcceptsWithoutCallingGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo, providerEnabled: false);
            var (message, bet, selections) = CreateCashoutInput();

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.Equal(0, auth.Calls);
            Assert.Null(repo.LastCashoutAssessmentRequest);
        }

        [Fact]
        public async Task ValidateCashoutAsync_AuthFails_Rejects()
        {
            var auth = new FakeGtsAuthRepository { Response = new BaseResponseDTO<ProviderAuthResponseDTO> { StatusCode = StatusCode.Error } };
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("1025 - GTS Authentication Failed", result.Reason);
            Assert.Null(repo.LastCashoutAssessmentRequest);
        }

        [Fact]
        public async Task ValidateCashoutAsync_AssessmentAllows_AcceptsAndPassesHasLive()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput(hasLive: true);

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.True(repo.LastIsInPlay);
            Assert.Same(bet, repo.LastCashoutAssessmentRequest!.Bet);
            Assert.Same(selections, repo.LastCashoutAssessmentRequest!.BetSelections);
        }

        [Fact]
        public async Task ValidateCashoutAsync_AssessmentDeclines_RejectsWithReason()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                CashoutAssessmentResponse = new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ProviderAssessmentResponseDTO { IsBetAllowed = false, Message = "Liability threshold breached" }
                }
            };
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("Liability threshold breached", result.Reason);
        }

        [Fact]
        public async Task ProcessCashout_FullCashout_SendsSettledBetUpdateWithCashoutFigures()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            var settlement = new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88", TrueStake = 100m, Payout = 80m },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1, BetSelectionStatusId = (int)BetStatus.CashedOut } }
            };

            await provider.ProcessCashout(settlement, CashoutType.Full, "corr-1");

            Assert.NotNull(repo.LastSendBet);
            Assert.True(repo.LastSendBet!.Value.IsSettled);
            Assert.Equal(100m, repo.LastSendBet!.Value.Cashout.CashedOutTotalStake);
            Assert.Equal(80m, repo.LastSendBet!.Value.Cashout.CashedOutAmount);
        }

        [Fact]
        public async Task ProcessCashout_PartialWithoutCumulativeFigures_DoesNotSendToGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            // Pre-enrichment message still in flight: no cumulative figures, must skip.
            await provider.ProcessCashout(new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88" },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1 } }
            }, CashoutType.Partial, "corr-1");

            Assert.Null(repo.LastSendBet);
        }

        [Fact]
        public async Task ProcessCashout_FullAfterPriorPartialsWithoutCumulativeFigures_DoesNotSendToGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            // Bet had earlier partial cashouts and the message carries no cumulative
            // figures: stake/payout alone understate what GTS requires, so skip.
            await provider.ProcessCashout(new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88", IsPartialCashout = true },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1 } }
            }, CashoutType.Full, "corr-1");

            Assert.Null(repo.LastSendBet);
        }

        [Fact]
        public async Task ProcessCashout_PartialWithCumulativeFigures_SendsOpenUpdateWithCumulativeTotals()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            // Second partial cashout of a 150 bet: first used 60 TrueStake (paid 55),
            // this one uses 37.5 (paid 33). Bet row already holds the reduced stake.
            var settlement = new OnBetSettlementPartial
            {
                Bet = new Bet
                {
                    BetId = Guid.NewGuid(),
                    ClientCode = "S88",
                    TrueStake = 52.5m,
                    Payout = 99.75m,
                    BetStatusId = (int)BetStatus.Running,
                    IsPartialCashout = true
                },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1, BetSelectionStatusId = (int)BetStatus.Running } },
                OriginalTrueStake = 150m,
                CashedOutTotalTrueStake = 97.5m,
                CashedOutTotalAmount = 88m
            };

            await provider.ProcessCashout(settlement, CashoutType.Partial, "corr-1");

            Assert.NotNull(repo.LastSendBet);
            Assert.False(repo.LastSendBet!.Value.IsSettled);
            Assert.Equal(97.5m, repo.LastSendBet!.Value.Cashout.CashedOutTotalStake);
            Assert.Equal(88m, repo.LastSendBet!.Value.Cashout.CashedOutAmount);
            // totalStake sent to GTS derives from Bet.TrueStake: must be the original stake.
            Assert.Equal(150m, repo.LastSendBet!.Value.Request.Bet.TrueStake);
        }

        [Fact]
        public async Task ProcessCashout_FullAfterPriorPartialsWithCumulativeFigures_SendsSettledCumulativeUpdate()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            // Final full cashout of the 52.5 remaining after 97.5 was cashed out in
            // partials (paid 88); this event pays 60. Payout reported to GTS is the
            // total paid across all cashouts, matching cashedOutAmount.
            var settlement = new OnBetSettlement
            {
                Bet = new Bet
                {
                    BetId = Guid.NewGuid(),
                    ClientCode = "S88",
                    TrueStake = 52.5m,
                    Payout = 60m,
                    BetStatusId = (int)BetStatus.CashedOut,
                    IsPartialCashout = true
                },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1, BetSelectionStatusId = (int)BetStatus.CashedOut } },
                OriginalTrueStake = 150m,
                CashedOutTotalTrueStake = 150m,
                CashedOutTotalAmount = 148m
            };

            await provider.ProcessCashout(settlement, CashoutType.Full, "corr-1");

            Assert.NotNull(repo.LastSendBet);
            Assert.True(repo.LastSendBet!.Value.IsSettled);
            Assert.Equal(150m, repo.LastSendBet!.Value.Cashout.CashedOutTotalStake);
            Assert.Equal(148m, repo.LastSendBet!.Value.Cashout.CashedOutAmount);
            Assert.Equal(150m, repo.LastSendBet!.Value.Request.Bet.TrueStake);
            Assert.Equal(148m, repo.LastSendBet!.Value.Request.Bet.Payout);
        }

        [Fact]
        public async Task ProcessCashout_AuthFails_DoesNotSendToGts()
        {
            var auth = new FakeGtsAuthRepository { Response = new BaseResponseDTO<ProviderAuthResponseDTO> { StatusCode = StatusCode.Error } };
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            await provider.ProcessCashout(new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88" },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1 } }
            }, CashoutType.Full, "corr-1");

            Assert.Null(repo.LastSendBet);
        }

        [Fact]
        public async Task ValidateCashoutAsync_AssessmentErrors_RejectsFailFast()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                CashoutAssessmentResponse = new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = "timeout"
                }
            };
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.StartsWith("1027", result.Reason);
        }

        [Fact]
        public async Task ValidateCashoutAsync_AssessmentDelayed_ReturnsPending()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                // GTS in-play bet delay: isBetAllowed=false + betAssessmentResult=AssessmentDelayed
                // is NOT a rejection — the verdict arrives via Ably (GM-767).
                CashoutAssessmentResponse = new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ProviderAssessmentResponseDTO { IsBetAllowed = false, IsPending = true }
                }
            };
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Pending, result.Acceptance);
        }

        [Fact]
        public async Task ValidateCashoutAsync_DelayedVerdictAccepted_SkipsAssessment()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();
            message.DelayedBetAcceptance = BetAcceptance.Accepted;

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.Null(repo.LastCashoutAssessmentRequest);
            Assert.Equal(0, auth.Calls);
        }

        [Fact]
        public async Task ValidateCashoutAsync_DelayedVerdictRejected_RejectsWithReason()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);
            var (message, bet, selections) = CreateCashoutInput();
            message.DelayedBetAcceptance = BetAcceptance.Rejected;
            message.DelayedMessage = "Liability threshold breached";

            var result = await provider.ValidateCashoutAsync(message, bet, selections, true, 0m, 0m);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("Liability threshold breached", result.Reason);
            Assert.Null(repo.LastCashoutAssessmentRequest);
        }
    }
}

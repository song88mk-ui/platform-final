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
        public async Task ProcessCashout_PartialCashoutType_DoesNotSendToGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            await provider.ProcessCashout(new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88" },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1 } }
            }, CashoutType.Partial, "corr-1");

            Assert.Null(repo.LastSendBet);
        }

        [Fact]
        public async Task ProcessCashout_FullAfterPriorPartials_DoesNotSendToGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var provider = CreateProvider(auth, repo);

            // Bet had earlier partial cashouts: stake/payout are no longer the cumulative
            // figures GTS requires, so the update must be skipped.
            await provider.ProcessCashout(new OnBetSettlement
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = "S88", IsPartialCashout = true },
                BetSelections = new List<BetSelection> { new BetSelection { SportId = 1 } }
            }, CashoutType.Full, "corr-1");

            Assert.Null(repo.LastSendBet);
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

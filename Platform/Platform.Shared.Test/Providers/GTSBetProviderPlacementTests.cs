using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Providers;
using Platform.Shared.Providers.GTS;

namespace Platform.Shared.Test.Providers
{
    // Covers the bet-placement half of GTSBetProvider: ValidateAsync, GetMaxStakeAsync,
    // IsAvailableAsync, ProcessSettlement and ProcessCancellation. GTSBetProviderCashoutTests
    // already covers the cashout half.
    //
    // ValidateAsync is the gate every bet passes through, and it has four ways of saying yes that
    // mean different things: the provider is switched off, the sport is virtual, GTS assessed and
    // allowed, or a delayed verdict already decided. Only the third actually consulted GTS. The
    // tests below keep those distinguishable, because collapsing them would let a bet through
    // unassessed while still looking accepted.
    public class GTSBetProviderPlacementTests
    {
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

        private static OnBetPlacement Placement(string clientCode = "S88", int sportId = 1, BetAcceptance? delayed = null, string? delayedMessage = null) =>
            new()
            {
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = clientCode, UserId = "user-1", LimitId = "700" },
                DelayedBetAcceptance = delayed,
                DelayedMessage = delayedMessage,
                BetSelections = new List<BetSelectionMessageExtenstion>
                {
                    new() { BetSelection = new BetSelection { SportId = sportId, Odds = 1.9m }, MarketSelectionCount = 2 }
                }
            };

        #region ValidateAsync

        [Fact]
        public async Task ValidateAsync_ProviderDisabled_AcceptsWithoutCallingGts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo, providerEnabled: false).ValidateAsync(Placement(), CancellationToken.None);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.Equal("1024 - Accepted (GTS disabled)", result.Reason);
            Assert.Equal(0, auth.Calls);
            Assert.Equal(0, repo.AssessmentCalls);
            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ValidateAsync_AuthFails_RejectsWithoutAssessing()
        {
            var auth = new FakeGtsAuthRepository();
            auth.FailWith(StatusCode.Error);
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo).ValidateAsync(Placement(), CancellationToken.None);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("1025 - GTS Authentication Failed", result.Reason);
            Assert.Equal(0, repo.AssessmentCalls);
        }

        [Fact]
        public async Task ValidateAsync_AssessmentAllows_SendsTheBetAndAccepts()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var placement = Placement();

            var result = await CreateProvider(auth, repo).ValidateAsync(placement, CancellationToken.None);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.Equal("allowed", result.Reason);
            Assert.Equal(1, repo.AssessmentCalls);
            Assert.Equal(1, repo.SendBetCalls);
            Assert.Equal(placement.Bet.BetId, repo.LastSendBet!.Value.Request.Bet.BetId);
            Assert.False(repo.LastSendBet!.Value.IsSettled);
        }

        [Fact]
        public async Task ValidateAsync_AssessmentDeclines_RejectsWithoutSending()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                AssessmentResponse = new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ProviderAssessmentResponseDTO { IsBetAllowed = false, Message = "liability exceeded" }
                }
            };

            var result = await CreateProvider(auth, repo).ValidateAsync(Placement(), CancellationToken.None);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("liability exceeded", result.Reason);
            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ValidateAsync_AssessmentPending_ReturnsPendingNotRejected()
        {
            // A pending assessment must stay distinguishable from a rejection: the bet is not
            // refused, it is awaiting a delayed verdict.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                AssessmentResponse = new BaseResponseDTO<ProviderAssessmentResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = new ProviderAssessmentResponseDTO { IsBetAllowed = false, IsPending = true, Message = "assessment delayed" }
                }
            };

            var result = await CreateProvider(auth, repo).ValidateAsync(Placement(), CancellationToken.None);

            Assert.Equal(BetAcceptance.Pending, result.Acceptance);
            Assert.Equal("assessment delayed", result.Reason);
            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ValidateAsync_SendBetFails_RejectsEvenThoughAssessmentAllowed()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                SendBetResponse = new BaseResponseDTO<string> { StatusCode = StatusCode.Error, Message = "boom" }
            };

            var result = await CreateProvider(auth, repo).ValidateAsync(Placement(), CancellationToken.None);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("1026 - Failed sending Bet to GTS boom", result.Reason);
        }

        [Fact]
        public async Task ValidateAsync_DelayedVerdictAccepted_SkipsAssessmentButStillSends()
        {
            // A delayed verdict means GTS already decided, so re-assessing would double-count the
            // bet. It must still be sent.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo)
                .ValidateAsync(Placement(delayed: BetAcceptance.Accepted, delayedMessage: "late yes"), CancellationToken.None);

            Assert.Equal(BetAcceptance.Accepted, result.Acceptance);
            Assert.Equal("late yes", result.Reason);
            Assert.Equal(0, repo.AssessmentCalls);
            Assert.Equal(1, repo.SendBetCalls);
        }

        [Fact]
        public async Task ValidateAsync_DelayedVerdictRejected_DoesNotSend()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo)
                .ValidateAsync(Placement(delayed: BetAcceptance.Rejected, delayedMessage: "late no"), CancellationToken.None);

            Assert.Equal(BetAcceptance.Rejected, result.Acceptance);
            Assert.Equal("late no", result.Reason);
            Assert.Equal(0, repo.AssessmentCalls);
            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ValidateAsync_TestClientCode_TagsTheRequestWithTheTestPrefix()
        {
            // Bets from a configured test client must reach the GTS test book, never the live one.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ValidateAsync(Placement(clientCode: "QTE"), CancellationToken.None);

            Assert.Equal("test", repo.LastAssessment!.Value.TestPrefix);
            Assert.Equal("test", repo.LastSendBetTestPrefix);
        }

        [Fact]
        public async Task ValidateAsync_TestClientCodeMatchIsCaseInsensitive()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ValidateAsync(Placement(clientCode: "qte"), CancellationToken.None);

            Assert.Equal("test", repo.LastAssessment!.Value.TestPrefix);
        }

        [Fact]
        public async Task ValidateAsync_LiveClientCode_SendsNoTestPrefix()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ValidateAsync(Placement(clientCode: "S88"), CancellationToken.None);

            Assert.Equal(string.Empty, repo.LastAssessment!.Value.TestPrefix);
            Assert.Equal(string.Empty, repo.LastSendBetTestPrefix);
        }

        [Fact]
        public async Task ValidateAsync_ForwardsEverySelection()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var placement = Placement();
            placement.BetSelections.Add(new BetSelectionMessageExtenstion
            {
                BetSelection = new BetSelection { SportId = 1, Odds = 2.4m }
            });

            await CreateProvider(auth, repo).ValidateAsync(placement, CancellationToken.None);

            Assert.Equal(2, repo.LastSendBet!.Value.Request.BetSelections.Count);
        }

        #endregion

        #region IsAvailableAsync

        [Fact]
        public async Task IsAvailableAsync_AuthSucceeds_ReturnsTrue()
        {
            var auth = new FakeGtsAuthRepository();

            Assert.True(await CreateProvider(auth, new FakeGtsRepository()).IsAvailableAsync(new BetRequestDTO()));
        }

        [Fact]
        public async Task IsAvailableAsync_AuthFails_ReturnsFalse()
        {
            var auth = new FakeGtsAuthRepository();
            auth.FailWith(StatusCode.Error);

            Assert.False(await CreateProvider(auth, new FakeGtsRepository()).IsAvailableAsync(new BetRequestDTO()));
        }

        [Fact]
        public async Task IsAvailableAsync_IgnoresTheProviderEnabledFlag()
        {
            // Availability reports whether GTS answers, not whether the platform is configured to
            // use it. Worth pinning: every other method short-circuits on that flag and this one
            // deliberately does not.
            var auth = new FakeGtsAuthRepository();

            Assert.True(await CreateProvider(auth, new FakeGtsRepository(), providerEnabled: false).IsAvailableAsync(new BetRequestDTO()));
        }

        #endregion

        #region GetMaxStakeAsync

        [Fact]
        public async Task GetMaxStakeAsync_Success_ReturnsTheStake()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo).GetMaxStakeAsync(new BetRequestDTO());

            Assert.True(result.IsSuccess);
            Assert.Equal(500m, result.MaxStake);
            Assert.Equal((int)StatusCode.Success, result.StatusCode);
            Assert.Equal(1, repo.MaxStakeCalls);
        }

        [Fact]
        public async Task GetMaxStakeAsync_AuthFails_ReturnsZeroStakeAndDoesNotCallGts()
        {
            // Zero, not null: a failed lookup must not read as an unlimited stake anywhere
            // downstream.
            var auth = new FakeGtsAuthRepository();
            auth.FailWith(StatusCode.Error);
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo).GetMaxStakeAsync(new BetRequestDTO());

            Assert.False(result.IsSuccess);
            Assert.Equal(0m, result.MaxStake);
            Assert.Equal("GTS_AUTH_FAILED", result.Message);
            Assert.Equal(0, repo.MaxStakeCalls);
        }

        [Fact]
        public async Task GetMaxStakeAsync_GtsFailsWithMessage_SurfacesThatMessage()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                MaxStakeResponse = new BaseResponseDTO<ProviderStakeResponseDTO> { StatusCode = StatusCode.Error, Message = "limit service down" }
            };

            var result = await CreateProvider(auth, repo).GetMaxStakeAsync(new BetRequestDTO());

            Assert.False(result.IsSuccess);
            Assert.Equal(0m, result.MaxStake);
            Assert.Equal("limit service down", result.Message);
        }

        [Fact]
        public async Task GetMaxStakeAsync_GtsFailsWithoutMessage_FallsBackToAGenericReason()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository
            {
                MaxStakeResponse = new BaseResponseDTO<ProviderStakeResponseDTO> { StatusCode = StatusCode.Error, Message = string.Empty }
            };

            var result = await CreateProvider(auth, repo).GetMaxStakeAsync(new BetRequestDTO());

            Assert.Equal("GTS_MAXSTAKE_FAILED", result.Message);
        }

        [Fact]
        public async Task GetMaxStakeAsync_IgnoresTheProviderEnabledFlag()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            var result = await CreateProvider(auth, repo, providerEnabled: false).GetMaxStakeAsync(new BetRequestDTO());

            Assert.True(result.IsSuccess);
            Assert.Equal(1, repo.MaxStakeCalls);
        }

        #endregion

        #region ProcessSettlement

        private static OnBetSettlement Settlement(bool isAudit = true, string clientCode = "S88", int sportId = 1) =>
            new()
            {
                IsAudit = isAudit,
                Bet = new Bet { BetId = Guid.NewGuid(), ClientCode = clientCode, UserId = "user-1", LimitId = "700" },
                BetSelections = new List<BetSelection> { new() { SportId = sportId, Odds = 1.9m } }
            };

        [Fact]
        public async Task ProcessSettlement_ProviderDisabled_SendsNothing()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo, providerEnabled: false).ProcessSettlement(Settlement(), "corr-1");

            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ProcessSettlement_IsAuditFalse_SendsNothing()
        {
            // GM-800: a settlement that did not change the bet must not reach GTS, or a Won leg on
            // an already-Lost parlay would be forwarded as a status change.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ProcessSettlement(Settlement(isAudit: false), "corr-1");

            Assert.Equal(0, repo.SendBetCalls);
            Assert.Equal(0, auth.Calls);
        }

        [Fact]
        public async Task ProcessSettlement_Changed_SendsAsSettled()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var settlement = Settlement();

            await CreateProvider(auth, repo).ProcessSettlement(settlement, "corr-1");

            Assert.Equal(1, repo.SendBetCalls);
            Assert.True(repo.LastSendBet!.Value.IsSettled);
            Assert.Equal(settlement.Bet.BetId, repo.LastSendBet!.Value.Request.Bet.BetId);
        }

        [Fact]
        public async Task ProcessSettlement_AuthFails_SendsNothing()
        {
            var auth = new FakeGtsAuthRepository();
            auth.FailWith(StatusCode.Error);
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ProcessSettlement(Settlement(), "corr-1");

            Assert.Equal(0, repo.SendBetCalls);
        }

        [Fact]
        public async Task ProcessSettlement_TestClientCode_TagsTheRequest()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ProcessSettlement(Settlement(clientCode: "KRJ"), "corr-1");

            Assert.Equal("test", repo.LastSendBetTestPrefix);
        }

        [Fact]
        public async Task ProcessSettlement_LiveClientCode_SendsNoTestPrefix()
        {
            // The negative half of the test-prefix rule: a live settlement must never be routed
            // to the GTS test book.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            await CreateProvider(auth, repo).ProcessSettlement(Settlement(clientCode: "S88"), "corr-1");

            Assert.Equal(string.Empty, repo.LastSendBetTestPrefix);
        }

        #endregion

        #region ProcessCancellation

        [Fact]
        public void ProcessCancellation_ProviderDisabled_CallsNothing()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            CreateProvider(auth, repo, providerEnabled: false)
                .ProcessCancellation(Guid.NewGuid(), "700", "S88", "corr-1");

            Assert.Equal(0, auth.Calls);
            Assert.Equal(0, repo.CancellationCalls);
        }

        [Fact]
        public async Task ProcessCancellation_ForwardsTheCancellation()
        {
            // ProcessCancellation is async void, so there is nothing to await. The fakes complete
            // synchronously, but yield once so the continuation has run before asserting.
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();
            var betId = Guid.NewGuid();

            CreateProvider(auth, repo).ProcessCancellation(betId, "700", "S88", "corr-1");
            await Task.Yield();

            Assert.Equal(1, repo.CancellationCalls);
            Assert.Equal(betId, repo.LastCancellation!.Value.BetId);
            Assert.Equal("700", repo.LastCancellation!.Value.LimitId);
            Assert.Equal(string.Empty, repo.LastCancellation!.Value.TestPrefix);
        }

        [Fact]
        public async Task ProcessCancellation_TestClientCode_TagsTheRequest()
        {
            var auth = new FakeGtsAuthRepository();
            var repo = new FakeGtsRepository();

            CreateProvider(auth, repo).ProcessCancellation(Guid.NewGuid(), "700", "QTE", "corr-1");
            await Task.Yield();

            Assert.Equal("test", repo.LastCancellation!.Value.TestPrefix);
        }

        [Fact]
        public async Task ProcessCancellation_AuthFails_DoesNotCancel()
        {
            var auth = new FakeGtsAuthRepository();
            auth.FailWith(StatusCode.Error);
            var repo = new FakeGtsRepository();

            CreateProvider(auth, repo).ProcessCancellation(Guid.NewGuid(), "700", "S88", "corr-1");
            await Task.Yield();

            Assert.Equal(0, repo.CancellationCalls);
        }

        #endregion
    }
}

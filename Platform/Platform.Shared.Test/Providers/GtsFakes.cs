using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using Platform.Shared.Repositories;

namespace Platform.Shared.Test.Providers
{
    // Hand-written doubles for the two interfaces GTSBetProvider depends on, shared by every GTS
    // provider suite. No mocking framework, matching the convention in
    // Platform.Sportsbook.API.Test/Repository/FakeMemCache.cs.
    //
    // Every member records what it was called with rather than throwing, because the provider's
    // value is mostly in what it forwards to GTS and what it declines to forward: the assertions
    // are about the recorded request, not just the returned result.

    internal sealed class FakeGtsAuthRepository : IGTSAuthRepository
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

        public void FailWith(StatusCode statusCode) =>
            Response = new BaseResponseDTO<ProviderAuthResponseDTO> { StatusCode = statusCode };
    }

    internal sealed class FakeGtsRepository : IGTSRepository
    {
        // Assessment

        public BaseResponseDTO<ProviderAssessmentResponseDTO> AssessmentResponse { get; set; } = new()
        {
            StatusCode = StatusCode.Success,
            Result = new ProviderAssessmentResponseDTO { IsBetAllowed = true, Message = "allowed" }
        };

        public (OnBetPlacement Request, string TestPrefix)? LastAssessment { get; private set; }
        public int AssessmentCalls { get; private set; }

        public Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request, string testPrefix = "")
        {
            AssessmentCalls++;
            LastAssessment = (request, testPrefix);
            return Task.FromResult(AssessmentResponse);
        }

        // Cashout assessment

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

        // SendBet

        public BaseResponseDTO<string> SendBetResponse { get; set; } = new() { StatusCode = StatusCode.Success };

        public (SendBet Request, bool IsSettled, GtsCashout Cashout)? LastSendBet { get; private set; }
        public string? LastSendBetTestPrefix { get; private set; }
        public int SendBetCalls { get; private set; }

        public Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request, string testPrefix = "", bool isSettled = false, GtsCashout cashout = null)
        {
            SendBetCalls++;
            LastSendBet = (request, isSettled, cashout);
            LastSendBetTestPrefix = testPrefix;
            return Task.FromResult(SendBetResponse);
        }

        // Max stake

        public BaseResponseDTO<ProviderStakeResponseDTO> MaxStakeResponse { get; set; } = new()
        {
            StatusCode = StatusCode.Success,
            Result = new ProviderStakeResponseDTO { Stake = 500m }
        };

        public BetRequestDTO? LastMaxStakeRequest { get; private set; }
        public int MaxStakeCalls { get; private set; }

        public Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request)
        {
            MaxStakeCalls++;
            LastMaxStakeRequest = request;
            return Task.FromResult(MaxStakeResponse);
        }

        public Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request) =>
            throw new NotImplementedException("GetMaxStakeOld is not called by GTSBetProvider");

        // Cancellation

        public (Guid BetId, string LimitId, string ClientCode, string TestPrefix)? LastCancellation { get; private set; }
        public int CancellationCalls { get; private set; }

        public Task<BaseResponseDTO<string>> Cancellation(string authToken, Guid betId, string limitId, string clientCode, string testPrefix = "")
        {
            CancellationCalls++;
            LastCancellation = (betId, limitId, clientCode, testPrefix);
            return Task.FromResult(new BaseResponseDTO<string> { StatusCode = StatusCode.Success });
        }
    }
}

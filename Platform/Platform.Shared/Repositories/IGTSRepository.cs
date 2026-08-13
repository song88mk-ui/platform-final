using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IGTSRepository
    {
        Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request);
        Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request);
        Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request, string testPrefix = "");
        Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> CashoutAssessment(string authToken, OnBetSettlement request, string testPrefix = "", bool isInPlay = false, List<CashedOutProbabilities> probabilities = null);
        Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request, string testPrefix = "", bool isSettled = false, GtsCashout cashout = null);
        Task<BaseResponseDTO<string>> Cancellation(string authToken, Guid betId, string limitId, string clientCode, string testPrefix = "");
    }
}

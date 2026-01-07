using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.GTS;
using System;
using System.Threading.Tasks;

namespace Platform.Shared.Repositories
{
    public interface IGTSRepository
    {
        Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStakeOld(string authToken, BetRequestDTO request);
        Task<BaseResponseDTO<ProviderStakeResponseDTO>> GetMaxStake(string authToken, BetRequestDTO request);
        Task<BaseResponseDTO<ProviderAssessmentResponseDTO>> Assessment(string authToken, OnBetPlacement request);
        Task<BaseResponseDTO<string>> SendBet(string authToken, SendBet request);
        Task<BaseResponseDTO<Task>> Cancellation(string authToken, Guid betId);
    }
}

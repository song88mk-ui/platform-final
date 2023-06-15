using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBetConfigRepository
    {
        Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO payload);
        Task<BaseResponseDTO<StatusCode>> ProcessUserBetConfig(UserBetConfigRequestDTO payload);
        Task<BaseResponseDTO<StatusCode>> ProcessParlayBonus(ParlayBonusRequestDTO payload);
        Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetClientParlayBonus(GetParlayBonusRequestDTO request);
    }
}

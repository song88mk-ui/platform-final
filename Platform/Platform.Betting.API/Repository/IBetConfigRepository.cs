using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IBetConfigRepository
    {
        Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO payload);
        Task<BaseResponseDTO<StatusCode>> ProcessMultibetBonus(MultibetBonusRequestDTO payload);
        Task<BaseResponseDTO<MultibetBonusResponseDTO>> GetClientMultibetBonus(GetMultibetBonusRequestDTO request);
        Task<BaseResponseDTO<MultibetBonusAuditResponseDTO>> GetClientMultibetBonusHistory(GetMultibetBonusRequestDTO request);
    }
}

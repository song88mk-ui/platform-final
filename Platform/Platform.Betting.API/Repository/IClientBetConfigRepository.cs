using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IClientBetConfigRepository
    {
        Task<BaseResponseDTO<StatusCode>> ProcessClientBetConfig(BackOfficeBetConfigPayloadDTO payload);
    }
}

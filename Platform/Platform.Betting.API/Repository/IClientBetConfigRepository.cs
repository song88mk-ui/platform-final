using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IClientBetConfigRepository
    {
        Task<IActionResult> ProcessConfig(BackOfficeBetConfigDTO request);
    }
}

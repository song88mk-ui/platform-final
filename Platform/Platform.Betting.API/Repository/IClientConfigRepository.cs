using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public interface IClientConfigRepository
    {
        Task<IActionResult> ProcessConfig(ClientConfigRequestDTO request);
    }
}

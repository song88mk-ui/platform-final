using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Betting.API.Repository;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class ConfigController : ControllerBase
    {
        private readonly IClientBetConfigRepository _clientbetRepository;

        public ConfigController(IClientBetConfigRepository clientbetRepository)
        {
            _clientbetRepository = clientbetRepository;
        }

        [HttpPost("v1/config/client")]
        public async Task<IActionResult> ProcessBackOfficeClientBetConfig([FromBody] BackOfficeBetConfigDTO request)
        {
            return await _clientbetRepository.ProcessConfig(request);
        }
    }
}
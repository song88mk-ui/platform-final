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
        private readonly IClientConfigRepository _clientRepository;

        public ConfigController(IClientConfigRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        [HttpPost("v1/config/client")]
        public async Task<IActionResult> ProcessClientConfig([FromBody] ClientConfigRequestDTO request)
        {
            return await _clientRepository.ProcessConfig(request);
        }
    }
}
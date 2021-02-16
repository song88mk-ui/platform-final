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
        private readonly IClientBetConfigRepository _clientbetconfigRepository;

        public ConfigController(IClientBetConfigRepository clientbeconfigRepository)
        {
            _clientbetconfigRepository = clientbeconfigRepository;
        }

        [HttpPost("v1/config/client")]
        public async Task<IActionResult> ProcessBackOfficeClientBetConfig([FromBody] BackOfficeBetConfigPayloadDTO request)
        {
            var result = await _clientbetconfigRepository.ProcessClientBetConfig(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
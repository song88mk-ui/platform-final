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
        private readonly IBetConfigRepository _clientbetconfigRepository;

        public ConfigController(IBetConfigRepository clientbeconfigRepository)
        {
            _clientbetconfigRepository = clientbeconfigRepository;
        }

        [HttpPost("v1/config/client")]
        public async Task<IActionResult> ProcessBackOfficeClientBetConfig([FromBody] BackOfficeBetConfigPayloadDTO payload)
        {
            var result = await _clientbetconfigRepository.ProcessBetConfig(payload);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/config/parlaybonus")]
        public async Task<IActionResult> ProcessBackOfficeClientParlayBonus([FromBody] ParlayBonusRequestDTO payload)
        {
            var result = await _clientbetconfigRepository.ProcessParlayBonus(payload);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/config/parlaybonus/list")]
        public async Task<IActionResult> GetClientParlayBonus([FromBody] GetParlayBonusRequestDTO request)
        {
            var result = await _clientbetconfigRepository.GetClientParlayBonus(request);

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
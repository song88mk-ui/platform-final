using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Betting.API.Repository;
using Platform.Shared.Models;
using System.Text.Json;
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

        [HttpPost("v1/config/multibetbonus")]
        public async Task<IActionResult> ProcessBackOfficeClientMultibetBonus([FromBody] MultibetBonusRequestDTO payload)
        {
            var result = await _clientbetconfigRepository.ProcessMultibetBonus(payload);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/config/multibetbonus/list")]
        public async Task<IActionResult> GetClientMultibetBonus([FromBody] GetMultibetBonusRequestDTO request)
        {
            var result = await _clientbetconfigRepository.GetClientMultibetBonus(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/config/multibetbonus/history/list")]
        public async Task<IActionResult> GetClientMultibetBonusHistory([FromBody] GetMultibetBonusRequestDTO request)
        {
            var result = await _clientbetconfigRepository.GetClientMultibetBonusHistory(request);

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
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using Platform.Sportsbook.API.Repository;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class SportsbookController : ControllerBase
    {
        private readonly ISportsbookRepository _sportsbookRepository;

        public SportsbookController(ISportsbookRepository sportsbookRepository)
        {
            _sportsbookRepository = sportsbookRepository;
        }

        [HttpPost("v1/sportsbook/livestream")]
        public async Task<IActionResult> GetLivestream([FromBody] LivestreamRequestDTO request)
        {
            var result = await _sportsbookRepository.GetLivestream(request);

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
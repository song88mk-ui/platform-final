using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.API.Repository;

namespace Platform.API.Controllers
{
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        [HttpGet("v1/wallet/balance")]
        public async Task<IActionResult> GetWalletBalance()
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.GetWalletBalance(clientId, operatorToken);

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
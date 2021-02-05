using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.API.ActionFilters;
using Platform.API.Repository;

namespace Platform.API.Controllers
{
    [ApiController]
    public class IntegrationController : BaseController
    {
        private readonly IPlatformRepository _integrationRepository;

        public IntegrationController(IPlatformRepository integrationRepository)
        {
            _integrationRepository = integrationRepository;
        }

        [HttpGet("v1/wallet/balance"), ServiceFilter(typeof(WalletActionFilter))]
        public async Task<IActionResult> GetWalletBalance()
        {
            return Ok(await _integrationRepository.GetWalletBalance(Balance));
        }
    }
}
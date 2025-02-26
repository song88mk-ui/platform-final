using Microsoft.AspNetCore.Mvc;
using Platform.RedCatBetting.API.Repositories;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCatBetting.API.Controllers
{
    [Route("v1/vs/bet")]
    [ApiController]
    public class BettingController : ControllerBase
    {
        private readonly IBettingRepository _bettingRepositories;

        public BettingController(IBettingRepository bettingRepositories)
        {
            _bettingRepositories = bettingRepositories;
        }

        [HttpPost("placement")]
        public async Task<IActionResult> PlaceBet(RedCatBetRequest request)
        {
            var result = await _bettingRepositories.PlaceBet(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("settle")]
        public async Task<IActionResult> SettleBet(RedCatBetRequest request)
        {
            var result = await _bettingRepositories.SettleBet(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("void/placement")]
        public async Task<IActionResult> VoidPlacement(RedCatVoidBetRequest request)
        {
            var result = await _bettingRepositories.VoidPlacement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("void/placement")]
        public async Task<IActionResult> VoidSettlement(RedCatVoidBetRequest request)
        {
            var result = await _bettingRepositories.VoidSettlement(request);

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

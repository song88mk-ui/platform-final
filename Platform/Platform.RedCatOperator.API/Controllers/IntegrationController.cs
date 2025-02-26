using Microsoft.AspNetCore.Mvc;
using Platform.RedCatOperator.API.Repositories;
using Platform.Shared.Models.RedCat;
using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.RedCatOperator.API.Controllers
{
    [Route("v1/vs")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IOperatorRepository _operatorRepository;

        public IntegrationController(OperatorRepository operatorRepository)
        {
            _operatorRepository = operatorRepository;
        }

        [HttpPost("bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] RedCatOperatorBetTransactionRequest request)
        {
            var result = await _operatorRepository.ProcessBetPlacement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/settle")]
        public async Task<IActionResult> SettleBet([FromBody] RedCatOperatorBetTransactionRequest request)
        {
            var result = await _operatorRepository.ProcessSettleBet(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/void/placement")]
        public async Task<IActionResult> VoidPlacement([FromBody] RedCatOperatorBetTransactionRequest request)
        {
            var result = await _operatorRepository.ProcessVoidPlacement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("bet/void/settle")]
        public async Task<IActionResult> VoidSettlement([FromBody] RedCatOperatorBetTransactionRequest request)
        {
            var result = await _operatorRepository.ProcessVoidSettlement(request);

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

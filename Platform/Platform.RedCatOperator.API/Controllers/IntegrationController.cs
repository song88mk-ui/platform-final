using Microsoft.AspNetCore.Mvc;
using Platform.RedCatOperator.API.Repositories;
using Platform.Shared.Models.RedCat;
using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.RedCatOperator.API.Controllers
{
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IOperatorRepository _operatorRepository;

        public IntegrationController(OperatorRepository operatorRepository)
        {
            _operatorRepository = operatorRepository;
        }

        [HttpPost("api/vs/bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] RedCatBetRequest request)
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
    }
}

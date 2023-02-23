using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.Repository;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class OperatorController : Controller
    {
        private readonly IOperatorRepository _operatorRepository;
        public OperatorController(IOperatorRepository operatorRepository)
        {
            _operatorRepository = operatorRepository;
        }

        [HttpPost("operator/v1/authentication")]
        public async Task<IActionResult> Authenticate()
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _operatorRepository.Authenticate(clientId, operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result.StatusId);
            }
        }
    }
}

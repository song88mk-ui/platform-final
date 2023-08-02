using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Platform.Operator.API.ActionFilters;
using Platform.Operator.API.Repository;
using Platform.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Operator.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;

        public IntegrationController(IPlatformRepository platformRepository)
        {
            //
            _platformRepository = platformRepository;
        }

        [HttpPost("v1/authentication")]
        public async Task<IActionResult> Authenticate()
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.Authenticate(operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result.StatusId);
            }
        }

        [HttpPost("v1/cashout/status")]
        public async Task<IActionResult> CashOutResult([FromBody] CashoutResultRequestDTO request)
        {
            var result = await _platformRepository.CashOutResult(request);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result);
            }
        }

        [HttpPost("v1/wallet/balance")]
        public async Task<IActionResult> WalletBalance()
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.WalletBalance(operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result);
            }
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> BetPlacement([FromBody] OperatorBetRequestDTO request)
        {
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.BetPlacement(request, operatorToken, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/full/cashout")]
        public async Task<IActionResult> BetFullCashOut([FromBody] OperatorBetSettlementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var result = await _platformRepository.BetFullCashOut(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/settlement")]
        public async Task<IActionResult> BetSettlement([FromBody] OperatorBetSettlementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var result = await _platformRepository.BetSettlement(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/cancellation")]
        public async Task<IActionResult> BetCancellation([FromBody] OperatorBetCancellationRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var result = await _platformRepository.BetCancellation(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/fixture/score")]
        public async Task<IActionResult> FixtureScore([FromBody] FixtureScoreRequestDTO request)
        {
            var result = await _platformRepository.FixtureScore(request);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return new BadRequestObjectResult(result);
            }
        }

        [HttpPost("v1/user")]
        [ServiceFilter(typeof(InternalAPIKeyFilter))]
        public async Task<IActionResult> AddUser([FromBody] GenerateUserRequestDTO request)
        {
            var result = await _platformRepository.AddUser(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/negativetest/add")]
        public async Task<IActionResult> AddClient([FromBody] string clientId)
        {
            var result = _platformRepository.AddClient(clientId);

            if (result == false)
                return BadRequest();

            return Ok();
        }

        [HttpPost("v1/negativetest/remove")]
        public async Task<IActionResult> RemoveClient([FromBody] string clientId)
        {
            var result = _platformRepository.RemoveClient(clientId);

            if (result == false)
                return BadRequest();

            return Ok();
        }

        [HttpPost("v1/negativetest/get")]
        public async Task<IActionResult> GetClients()
        {
            var result = _platformRepository.GetClients();

            return Ok(result);
        }

        [HttpPost("v1/testconfig/add")]
        public async Task<IActionResult> AddTestConfig([FromQuery] string clientId, [FromQuery] bool isFreeBet, [FromQuery] decimal stakeRealPercent, [FromQuery] decimal stakeBonusPercent)
        {
            var result = _platformRepository.AddTestConfig(clientId, isFreeBet, stakeRealPercent, stakeBonusPercent);

            if (result == false)
                return BadRequest();

            return Ok();
        }

        [HttpPost("v1/testconfig/remove")]
        public async Task<IActionResult> RemoveTestConfig([FromQuery] string clientId)
        {
            var result = _platformRepository.RemoveTestConfig(clientId);

            if (result == false)
                return BadRequest();

            return Ok();
        }

        [HttpPost("v1/testconfig/get")]
        public async Task<IActionResult> GetTestConfig()
        {
            var result = _platformRepository.GetTestConfig();

            return Ok(result);
        }

        [HttpPost("v1/casino/bet/placement")]
        public async Task<IActionResult> CasinoBetPlacement([FromBody] OperatorCasinoBetPlacementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.BetPlacementCasino(request, clientId, operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/casino/bet/settlement")]
        public async Task<IActionResult> CasinoBetSettlement([FromBody] OperatorCasinoBetSettlementRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.BetSettlementCasino(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v1/esports/debit")]
        public async Task<IActionResult> ESportsDebit([FromBody] OperatorESportsDebitRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var result = await _platformRepository.DebitESports(request, clientId, operatorToken);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/esports/credit")]
        public async Task<IActionResult> ESportsCredit([FromBody] OperatorESportsCreditRequestDTO request)
        {
            var clientId = HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            var result = await _platformRepository.CreditESports(request, clientId);

            switch (result.StatusId)
            {
                case 1000:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

    }
}
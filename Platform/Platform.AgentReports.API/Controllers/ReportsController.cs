using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Repository;
using Platform.Shared.Enums;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsRepository _reportsRepository;

        public ReportsController(IReportsRepository reportsRepository)
        {
            _reportsRepository = reportsRepository;
        }

        [HttpPost("v1/reports/winloss/{agentId}/{isAgent}")]
        public async Task<IActionResult> GetWinLoss(long agentId, bool isAgent, [FromBody] WinLossAccountRequest request)
        {
            var result = await _reportsRepository.GetWinLoss(agentId, isAgent, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/winloss/{agentId}/{directUserId}/{isAgent}")]
        public async Task<IActionResult> GetWinLoss(long agentId, long directUserId, bool isAgent, [FromBody] WinLossAccountRequestExtended request)
        {
            var result = await _reportsRepository.GetWinLoss(agentId, directUserId, isAgent, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/winloss/details/{agentId}/{directUserId}/{isAgent}")]
        public async Task<IActionResult> GetWinLossDetails(long agentId, long directUserId, bool isAgent, [FromBody] WinLossAccountRequestExtended request)
        {
            var result = await _reportsRepository.GetWinLossDetails(agentId, directUserId, isAgent, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/allbets")]
        public async Task<IActionResult> GetAllBets([FromBody] SearchBetRequest request)
        {
            var result = await _reportsRepository.GetAllBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/parlaybets")]
        public async Task<IActionResult> GetParlayBets([FromBody] SearchBetRequest request)
        {
            var result = await _reportsRepository.GetParlayBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/partner/revenue/monthly/{userId}/{year}")]
        public async Task<IActionResult> GetPartnerMonthlyRevenue(long userId, int year, [FromQuery] string currencyCode)
        {
            var result = await _reportsRepository.GetPartnerMonthlyGGR(userId, year, currencyCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/bets/outstanding/fixtures/{agentId}/{page}/{pageSize}")]
        public async Task<IActionResult> GetOutstandingBetFixtures(long agentId, int page, int pageSize, [FromQuery] Currency currency)
        {
            var result = await _reportsRepository.GetOutstandingBetFixtures(agentId, page, pageSize, currency);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v2/reports/profitloss/{agentId}")]
        public IActionResult GetProfitLossV2(long agentId, [FromBody] ProfitLossRequest request)
        {
            return Ok(_reportsRepository.GetProfitLossV2(agentId, request));
        }

        [HttpPost("v2/reports/profitloss/{agentId}/{directUserId}")]
        public IActionResult GetProfitLossV2(long agentId, long directUserId, [FromBody] ProfitLossRequest request)
        {
            return Ok(_reportsRepository.GetProfitLossV2(agentId, directUserId, request));
        }

        [HttpPost("v2/reports/profitloss/details/{agentId}/{directUserId}")]
        public IActionResult GetProfitLossDetailsV2(long agentId, long directUserId, [FromBody] ProfitLossRequest request)
        {
            return Ok(_reportsRepository.GetProfitLossDetailsV2(agentId, directUserId, request));
        }

        [HttpPost("v2/reports/ggr/{agentId}/{directAgentId}")]
        public IActionResult GetGGRV2(long agentId, long directAgentId, [FromBody] WinLossAccountRequestExtendedDTO request)
        {
            return Ok(_reportsRepository.GetGGRV2(agentId, directAgentId, request));
        }

        [HttpPost("v2/reports/ggr/details/{agentId}/{playerId}")]
        public IActionResult GetGGRDetailsV2(long agentId, long playerId, [FromBody] WinLossAccountRequestExtendedDTO request)
        {
            return Ok(_reportsRepository.GetGGRDetailsV2(agentId, playerId, request));
        }
    }
}

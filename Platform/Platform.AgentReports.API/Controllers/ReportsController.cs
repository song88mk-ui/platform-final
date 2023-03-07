using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Repository;
using Platform.Shared.Enums;
using System;
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

        [HttpGet("v1/reports/partner/revenue/monthly/{userId}/{year}")]
        public async Task<IActionResult> GetPartnerMonthlyRevenue(long userId, int year, [FromQuery] string currencyCode)
        {
            return Ok();
        }

        [HttpGet("v1/bets/outstanding/fixtures/{agentId}/{page}/{pageSize}")]
        public async Task<IActionResult> GetOutstandingBetFixtures(long agentId, int page, int pageSize, [FromQuery] CurrencyCode currency)
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

        #region Profit Loss

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
        #endregion
        #region GGR

        [HttpPost("v2/reports/ggr/{agentId}/{directAgentId}")]
        public IActionResult GetGGRV2(long agentId, long directAgentId, [FromBody] WinLossAccountRequestExtended request)
        {
            return Ok(_reportsRepository.GetGGRV2(agentId, directAgentId, request));
        }

        [HttpPost("v2/reports/ggr/details/{agentId}/{playerId}")]
        public IActionResult GetGGRDetailsV2(long agentId, long playerId, [FromBody] WinLossAccountRequestExtended request)
        {
            return Ok(_reportsRepository.GetGGRDetailsV2(agentId, playerId, request));
        }
        #endregion
    }
}

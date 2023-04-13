using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.ActionFilters;
using Platform.AgentReports.API.Models;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Repository;
using Platform.Shared.Enums;
using System;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(APIKeyFilter))]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsRepository _reportsRepository;

        public ReportsController(IReportsRepository reportsRepository)
        {//
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

        [HttpPost("v1/reports/totalbets")]
        public async Task<IActionResult> GetTotalBets([FromBody] TotalBetsRequest request)
        {
            var result = await _reportsRepository.GetTotalBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/totalbets/others")]

        public async Task<IActionResult> GetTotalBetsOthers([FromBody] TotalBetsRequest request)
        {
            var result = await _reportsRepository.GetTotalBetsOthers(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/bets/outstanding/downline")]
        public async Task<IActionResult> GetOutstandingBets([FromBody] OutstandingBetRequest request)
        {
            var result = await _reportsRepository.GetOutstandingBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/bets/history/{agentId}/{betId}")]
        public async Task<IActionResult> GetBetHistory(long agentId, Guid betId)
        {
            var result = await _reportsRepository.GetBetHistory(agentId, betId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/bets/history/profitloss/{agentId}/{betId}")]
        public async Task<IActionResult> GetBetHistoryProfitLoss(long agentId, Guid betId)
        {
            var result = await _reportsRepository.GetBetHistoryProfitLoss(agentId, betId);

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

        [HttpPost("v1/reports/profitloss/{agentId}")]
        public async Task<IActionResult> GetProfitLoss(long agentId, [FromBody] ProfitLossRequest request)
        {
            var result = await _reportsRepository.GetProfitLoss(agentId, request);
            return Ok(result);
        }

        [HttpPost("v1/reports/profitloss/{agentId}/{directUserId}")]
        public async Task<IActionResult> GetProfitLoss(long agentId, long directUserId, [FromBody] ProfitLossRequest request)
        {
            var result = await _reportsRepository.GetProfitLoss(agentId, directUserId, request);

            return Ok(result);
        }

        [HttpPost("v1/reports/profitloss/details/{agentId}/{directUserId}")]
        public async Task<IActionResult> GetProfitLossDetails(long agentId, long directUserId, [FromBody] ProfitLossRequest request)
        {
            var result = await _reportsRepository.GetProfitLossDetails(agentId, directUserId, request);
            return Ok(result);
        }

        [HttpPost("v1/reports/ggr/{agentId}/{directAgentId}")]
        public async Task<IActionResult> GetGGR(long agentId, long directAgentId, [FromBody] WinLossAccountRequestExtendedDTO request)
        {
            return Ok(await _reportsRepository.GetGGR(agentId, directAgentId, request));
        }

        [HttpPost("v1/reports/ggr/details/{agentId}/{playerId}")]
        public async Task<IActionResult> GetGGRDetails(long agentId, long playerId, [FromBody] WinLossAccountRequestExtendedDTO request)
        {
            return Ok(await _reportsRepository.GetGGRDetails(agentId, playerId, request));
        }

        [HttpPost("v1/reports/payment/transferhistory")]
        public async Task<IActionResult> GetPaymentTransferHistory([FromBody] TransferHistoryRequest request)
        {
            request.UserTypeId = request.UserTypeId == 10 ? (int)UserType.Agent : (int)UserType.Player;

            var result = await _reportsRepository.GetPaymentTransferHistory(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/partner/revenue/settlement")]
        public async Task<IActionResult> GetPartnerRevenueForSettlement([FromBody] PartnerRevenueSettlementRequest request)
        {
            var result = await _reportsRepository.GetPartnerRevenueForSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/partner/revenue/settlementhistory")]
        public async Task<IActionResult> GetPartnerRevenueForSettlementHistory([FromBody] PartnerRevenueSettlementHistoryRequest request)
        {
            var result = await _reportsRepository.GetPartnerRevenueForSettlementHistory(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/cancelbethistory")]
        public async Task<IActionResult> GetCancelBetHistory([FromBody] CancelBetHistoryRequest request)
        {
            var result = await _reportsRepository.GetCancelBetHistory(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/profile/{userId}")]
        public async Task<IActionResult> GetProfile(long userId)
        {
            var result = await _reportsRepository.GetProfile(userId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/runningbalance/{userId}")]
        public async Task<IActionResult> GetRunningBalance(string userId)
        {
            var result = await _reportsRepository.GetRunningBalance(userId);

            switch (result.Item1)
            {
                case 1000:
                    return Ok(new { StatusCode = result.Item1, Message = result.Item2, OutstandingBalance = result.Item3, CashBalance = result.Item4 });
                default:
                    return BadRequest(new { StatusCode = result.Item1, Message = result.Item2, OutstandingBalance = result.Item3, CashBalance = result.Item4 });
            }
        }

        [HttpPost("v1/reports/runningbalance")]
        public async Task<IActionResult> CreateRunningBalance([FromBody] CreateRunningBalanceRequest request)
        {
            var result = await _reportsRepository.CreateRunningBalance(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/reports/revenueshare")]
        public async Task<IActionResult> CreateRevenueShareConfig([FromBody] RevenueShareConfigRequest request)
        {
            var result = await _reportsRepository.CreateRevenueShareConfig(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("v1/reports/activemembers/{agentId}/{currency}")]
        public async Task<IActionResult> GetActiveMembers(long agentId, CurrencyCode currency, DateTime? fromDate, DateTime? toDate)
        {
            var result = await _reportsRepository.GetActiveMembers(agentId, currency, fromDate, toDate);

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

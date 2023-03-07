using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("v1/reports/allbets")]
        public async Task<IActionResult> GetAllBets()
        {            
            return Ok();
        }

        [HttpGet("v1/reports/partner/revenue/monthly/{userId}/{year}")]
        public async Task<IActionResult> GetPartnerMonthlyRevenue(long userId, int year, [FromQuery] string currencyCode)
        {
            return Ok();
        }

        [HttpGet("v1/bets/outstanding/fixtures/{agentId}/{page}/{pageSize}")]
        public async Task<IActionResult> GetOutstandingBetFixtures(long agentId, int page, int pageSize, [FromQuery] CurrencyCode currency)
        {
            return Ok();
        }
    }
}

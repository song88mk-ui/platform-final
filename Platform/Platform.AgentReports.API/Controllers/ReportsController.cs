using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.Repository;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsRepository _agentsReportsRepository;

        public ReportsController(IReportsRepository agentsReportsRepository)
        {
            _agentsReportsRepository = agentsReportsRepository;
        }

        [HttpPost("v1/reports/allbets")]
        public async Task<IActionResult> GetAllBets()
        {            
            return Ok();
        }
    }
}

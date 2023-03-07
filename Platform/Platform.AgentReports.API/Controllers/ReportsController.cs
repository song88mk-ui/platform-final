using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.AgentReports.API.Repository;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class ReportsController : ControllerBase
    {
        private readonly IAgentsReportsRepository _agentsReportsRepository;

        public ReportsController(IAgentsReportsRepository agentsReportsRepository)
        {
            _agentsReportsRepository = agentsReportsRepository;
        }

        [HttpGet("v1/reports/get")]
        public async Task<IActionResult> Get()
        {            
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Platform.AgentReports.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class ReportsController : ControllerBase
    {
        public ReportsController()
        {

        }

        [HttpGet("v1/reports/get")]
        public async Task<IActionResult> Get()
        {
            return Ok();
        }
    }
}

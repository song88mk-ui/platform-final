using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Oddin.API.Models;
using Platform.Shared.Enums;
using System;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        [HttpPost("v1/userdetails")]
        public async Task<IActionResult> UserDetails([FromBody] UserDetailsRequest request)
        {
            var result = new UserDetailsResponse
            {
                UserId = "2712cc4b-f158-404b-9aa9-10d01522b669",
                Currency = CurrencyCode.EUR.ToString(),
                Language = LanguageCode.EN.ToString(),
                ErrorCode = 0
            };

            return Ok(result);
        }

        [HttpPost("v1/debituser")]
        public async Task<IActionResult> DebitUser([FromBody] DebitUserRequest request)
        {
            var result = new DebitUserResponse
            {
                OperatorTransactionId = Guid.NewGuid().ToString(),
                ErrorCode = 0
            };

            return Ok(result);
        }

        [HttpPost("v1/credituser")]
        public async Task<IActionResult> CreditUser([FromBody] CreditUserRequest request)
        {
            var result = new CreditUserResponse
            {
                OperatorTransactionId = Guid.NewGuid().ToString(),
                ErrorCode = 0
            };

            return Ok(result);
        }
    }
}

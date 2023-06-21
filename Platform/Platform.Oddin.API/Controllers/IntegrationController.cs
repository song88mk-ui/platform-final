using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using Platform.Oddin.API.Repository;
using Platform.Shared.Enums;
using System;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly IESportsBettingRepository _eSportsBettingRepository;
        public IntegrationController(IESportsBettingRepository eSportsBettingRepository)
        {
            _eSportsBettingRepository = eSportsBettingRepository;
        }
        [HttpPost("v1/userdetails")]
        public async Task<IActionResult> UserDetails([FromBody] UserDetailsRequest request)
        {                        
            var result = await _eSportsBettingRepository.GetUserDetails(request);

            switch (result.ErrorCode)
            {
                case (int)SessionCode.OK:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/debituser")]
        public async Task<IActionResult> DebitUser([FromBody] DebitUserRequest request)
        {
            var result = new DebitUserResponse
            {
                OperatorTransactionId = Guid.NewGuid().ToString(),
                ErrorCode = 0
            };

            result = await _eSportsBettingRepository.DebitUser(request);

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

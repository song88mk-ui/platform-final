using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using Platform.Oddin.API.Repository;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly IESportsBettingRepository _eSportsBettingRepository;
        private readonly TelemetryClient _telemetryClient;

        public IntegrationController(IESportsBettingRepository eSportsBettingRepository, TelemetryClient telemetryClient)
        {
            _eSportsBettingRepository = eSportsBettingRepository;
            _telemetryClient = telemetryClient;
        }
        [HttpPost("v1/userdetails")]
        public async Task<IActionResult> UserDetails([FromBody] UserDetailsRequest request)
        {                        
            var result = await _eSportsBettingRepository.GetUserDetails(request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.ErrorCode.ToString() },
                                { "message", ((SessionCode)result.ErrorCode).ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/userdetails", properties);

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
            var result = await _eSportsBettingRepository.Withdraw(request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.ErrorCode.ToString() },
                                { "message", ((SessionCode)result.ErrorCode).ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/debituser", properties);

            switch ((SessionCode)result.ErrorCode)
            {
                case SessionCode.OK:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/credituser")]
        public async Task<IActionResult> CreditUser([FromBody] CreditUserRequest request)
        {
            var result = await _eSportsBettingRepository.Deposit(request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.ErrorCode.ToString() },
                                { "message", ((SessionCode)result.ErrorCode).ToString() }
                            };

            _telemetryClient.TrackEvent("e:v1/credituser", properties);

            switch ((SessionCode)result.ErrorCode)
            {
                case SessionCode.OK:
                    return Ok(result);
                default:
                    return BadRequest(result);
            };
        }
    }
}

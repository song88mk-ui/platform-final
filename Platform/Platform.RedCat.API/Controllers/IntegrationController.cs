using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.RedCat.API.Models;
using Platform.RedCat.API.Repositories;
using Platform.Shared.Enums;
using Platform.Shared.Models.RedCat;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    [Route("api/redcat")]
    public class IntegrationController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;
        private readonly TelemetryClient _telemetryClient;

        public IntegrationController(IPlatformRepository transactionRepository, TelemetryClient telemetryClient)
        {
            _platformRepository = transactionRepository;
            _telemetryClient = telemetryClient;
        }

        [HttpPost("validatetoken")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequestDTO request)
        {
            var result = await _platformRepository.ValidateToken(request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                //{ "ipAddress", ipAddress },
                                //{ "x-api-key", apiKey },
                                //{ "x-client-id", clientId },
                                //{ "user-agent", userAgent },
                                //{ "referer",  HttpContext.Request.Headers["X-Referer"].FirstOrDefault() ?? HttpContext.Request.Headers["Referer"].FirstOrDefault() ?? "none"},
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:api/redcat/validatetoken", properties);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }

        [HttpPost("getbalance")]
        public async Task<IActionResult> GetBalance([FromBody] WalletBalanceRequestDTO request)
        {
            var result = await _platformRepository.GetWalletBalance(request);

            var parameters = request;

            var properties = new Dictionary<string, string>
                            {
                                //{ "ipAddress", ipAddress },
                                //{ "x-api-key", apiKey },
                                //{ "x-client-id", clientId },
                                //{ "user-agent", userAgent },
                                //{ "referer",  HttpContext.Request.Headers["X-Referer"].FirstOrDefault() ?? HttpContext.Request.Headers["Referer"].FirstOrDefault() ?? "none"},
                                { "requestParameters", JsonSerializer.Serialize(parameters) },
                                { "statusCode", result.StatusCode.ToString() },
                                { "message", result.Message.ToString() },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:api/redcat/getbalance", properties);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }

        [HttpPost("getapproval")]
        public async Task<IActionResult> GetApproval([FromBody] ApprovalRequestDTO request)
        {
            var result = await _platformRepository.GetApproval(request);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }

        [HttpPost("stake")]
        public async Task<IActionResult> Stake([FromBody] RedCatBetTransactionRequestDTO request)
        {
            var result = await _platformRepository.Stake(request);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }

        [HttpPost("settle")]
        public async Task<IActionResult> Settle([FromBody] RedCatBetTransactionRequestDTO request)
        {
            var result = await _platformRepository.Settle(request);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }


        [HttpPost("voidstake")]
        public async Task<IActionResult> VoidStake([FromBody] RedCatVoidBetTransactionRequestDTO request)
        {
            var result = await _platformRepository.VoidStake(request);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }


        [HttpPost("voidsettle")]
        public async Task<IActionResult> VoidSettle([FromBody] RedCatVoidBetTransactionRequestDTO request)
        {
            var result = await _platformRepository.VoidSettle(request);

            switch (result.StatusCode)
            {
                case RedCatStatusCode.Success:
                    return Ok(result.Result);
                default:
                    return BadRequest(result.Result);
            }
        }
    }
}

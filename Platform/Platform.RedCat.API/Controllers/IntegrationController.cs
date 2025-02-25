using Microsoft.AspNetCore.Mvc;
using Platform.RedCat.API.Models;
using Platform.RedCat.API.Repositories;
using Platform.Shared.Enums;
using Platform.Shared.Models.RedCat;
using System.Threading.Tasks;

namespace Platform.RedCat.API.Controllers
{
    [Route("api/redcat")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;

        public IntegrationController(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        [HttpPost("validatetoken")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequestDTO request)
        {
            var result = await _transactionRepository.ValidateToken(request);

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
            var result = await _transactionRepository.GetWalletBalance(request);

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
            var result = await _transactionRepository.GetApproval(request);

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
            var result = await _transactionRepository.Stake(request);

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
            var result = await _transactionRepository.Settle(request);

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
            return Ok();
        }


        [HttpPost("voidsettle")]
        public async Task<IActionResult> VoidSettle()
        {
            return Ok();
        }
    }
}

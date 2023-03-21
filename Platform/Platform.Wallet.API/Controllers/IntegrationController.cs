using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Platform.Wallet.API.Models;
using Platform.Wallet.API.Repository;
using Platform.Shared.Models;

namespace Platform.Wallet.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : Controller
    {
        private readonly IWalletRepository _walletRepository;
        public IntegrationController(IWalletRepository walletRepository)
        {
            _walletRepository = walletRepository;
        }

        [HttpPost("v1/credit")]
        public async Task<IActionResult> Credit([FromBody] CreditOrDebitRequestDTO request)
        {
            var result = await _walletRepository.CreditUserWallet(request);

            if (result.Item1 == 1000)
                return Ok(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3 });
            else
                return BadRequest(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3 });
        }

        [HttpPost("v1/debit")]
        public async Task<IActionResult> Debit([FromBody] CreditOrDebitRequestDTO request)
        {
            var result = await _walletRepository.DebitUserWallet(request);

            if (result.Item1 == 1000)
                return Ok(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3 });
            else
                return BadRequest(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3 });
        }

        [HttpPost("v1/userwallettransfer")]
        public async Task<IActionResult> TransferWallet([FromBody] TransferUserWalletRequest request)
        {
            var result = await _walletRepository.TransferUserWallet(request);

            if (result.Item1 == 1000)
                return Ok(new { StatusCode = result.Item1, Message = result.Item2, FromBalance = result.Item3, ToBalance = result.Item4 });
            else
                return BadRequest(new { StatusCode = result.Item1, Message = result.Item2, FromBalance = result.Item3, ToBalance = result.Item4 });
        }

        [HttpGet("v1/userwallet")]
        public async Task<IActionResult> GetUserWallet([FromQuery] UserWalletRequest request)
        {
            var result = await _walletRepository.GetUserWallet(request);

            if (result.Item1 == 1000)
                return Ok(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3, CurrencyCode = result.Item4 });
            else
                return BadRequest(new { StatusCode = result.Item1, Message = result.Item2, Balance = result.Item3, CurrencyCode = result.Item4 });
        }
    }
}

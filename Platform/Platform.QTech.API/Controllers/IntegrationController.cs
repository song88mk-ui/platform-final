using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.QTech.API.Models;
using Platform.QTech.API.Repository;
using Platform.Shared.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.QTech.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly ICasinoRepository _casinoRepository;

        public IntegrationController(ICasinoRepository casinoRepository)
        {
            _casinoRepository = casinoRepository;
        }

        [HttpGet("v1/accounts/{playerId}/session")]
        public async Task<IActionResult> GetSession(string playerId, [FromQuery] string gameId)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            var result = await _casinoRepository.GetSession(new GameSessionRequest
            {
                WalletSession = walletSession,
                PassKey = passKey
            });

            switch (result.Item1)
            {
                case Enums.SessionCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.Item1.ToString() });
            }
        }

        [HttpGet("v1/accounts/{playerId}/balance")]
        public async Task<IActionResult> GetBalance(string playerId, [FromQuery] string gameId)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            var result = await _casinoRepository.GetBalance(new BalanceRequest
            {
                WalletSession = walletSession,
                PassKey = passKey
            });

            switch (result.Item1)
            {
                case Enums.BalanceCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.Item1.ToString() });
            }
        }

        [HttpPost("v1/transactions")]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequest request)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            if (request.TxnType.ToUpper() == "DEBIT")
            {
                var withdrawResponse = await _casinoRepository.Withdraw(new WithdrawRequest
                {
                    WalletSession = walletSession,
                    PassKey = passKey,
                    TxnType = request.TxnType,
                    TxnId = request.TxnId,
                    PlayerId = request.PlayerId,
                    RoundId = request.RoundId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    BonusBetAmount = request.BonusBetAmount,
                    BonusType = request.BonusType,
                    BonusPromoCode = request.BonusPromoCode,
                    JpContributions = request.JpContributions,
                    ConversionRate = request.ConversionRate,
                    GameId = request.GameId,
                    Device = request.Device,
                    ClientType = request.ClientType,
                    ClientRoundId = request.ClientRoundId,
                    Category = request.Category,
                    Created = request.Created,
                    Completed = request.Completed,
                    TableId = request.TableId
                });

                switch (withdrawResponse.Item1)
                {
                    case Enums.WithdrawCode.OK:
                        return Ok(withdrawResponse.Item2);
                    default:
                        return BadRequest(new { code = withdrawResponse.Item1.ToString() });
                }
            }
            else
            {
                var depositResponse = await _casinoRepository.Deposit(new DepositRequest
                {
                    WalletSession = walletSession,
                    PassKey = passKey,
                    TxnType = request.TxnType,
                    TxnId = request.TxnId,
                    PlayerId = request.PlayerId,
                    RoundId = request.RoundId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    BonusType = request.BonusType,
                    BonusPromoCode = request.BonusPromoCode,
                    JpPayout = request.JpPayout,
                    ConversionRate = request.ConversionRate,
                    GameId = request.GameId,
                    Device = request.Device,
                    ClientType = request.ClientType,
                    ClientRoundId = request.ClientRoundId,
                    Category = request.Category,
                    Created = request.Created,
                    Completed = request.Completed,
                    TableId = request.TableId
                });

                switch (depositResponse.Item1)
                {
                    case Enums.DepositCode.OK:
                        return Ok(depositResponse.Item2);
                    default:
                        return BadRequest(new { code = depositResponse.Item1.ToString() });
                }
            }
        }

        [HttpPost("v1/transactions/rollback")]
        public async Task<IActionResult> ProcessRollback([FromBody] RollbackRequest request)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            request.WalletSession = walletSession;
            request.PassKey = passKey;

            var result = await _casinoRepository.Rollback(request);

            switch (result.Item1)
            {
                case Enums.RollbackCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.ToString() });
            }
        }

        [HttpPost("v1/bonus/status")]
        public async Task<IActionResult> ProcessBonusStatus([FromBody] BonusStatusRequest request)
        {
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            request.PassKey = passKey;

            var result = await _casinoRepository.ProcessBonusStatus(request);

            switch (result)
            {
                case Enums.BonusStatusCode.OK:
                    return NoContent();
                default:
                    return BadRequest(new { code = result.ToString() });
            }
        }

        [HttpPost("v1/bonus/rewards")]
        public async Task<IActionResult> ProcessBonusReward([FromBody] BonusRewardRequest request)
        {
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();
            var contentType = HttpContext.Request.Headers["Content-Type"].FirstOrDefault();

            request.PassKey = passKey;
            request.ContentType = contentType;

            var result = await _casinoRepository.ProcessBonusReward(request);

            switch (result.Item1)
            {
                case Enums.BonusRewardCode.OK:
                    return NoContent();
                default:
                    return BadRequest(new { code = result.ToString() });
            }
        }

        [HttpPost("v1/lobby")]
        public async Task<IActionResult> GetLobbyURL([FromBody] GameLobbyRequestDTO request)
        {
            var result = await _casinoRepository.GetLobbyURL(request);

            switch (result.Item1)
            {
                case Enums.GameLobbyCode.OK:
                    return Ok(new BaseResponseDTO<GameLobbyResponseDTO>
                    {
                        StatusCode = Shared.Enums.StatusCode.Success,
                        Message = Shared.Enums.StatusCode.Success.ToString(),
                        Result = result.Item2
                    });
                default:
                    return BadRequest(new BaseResponseDTO<GameLobbyResponseDTO>
                    {
                        StatusCode = Shared.Enums.StatusCode.Error,
                        Message = Shared.Enums.StatusCode.Error.ToString()
                    });
            }
        }
    }
}

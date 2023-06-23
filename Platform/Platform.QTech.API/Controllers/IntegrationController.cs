using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.QTech.API.ActionFilters;
using Platform.QTech.API.Models;
using Platform.QTech.API.Repository;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.QTech.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class IntegrationController : ControllerBase
    {
        private readonly ICasinoRepository _casinoRepository;
        private readonly TelemetryClient _telemetryClient;

        public IntegrationController(ICasinoRepository casinoRepository, TelemetryClient telemetryClient)
        {
            _casinoRepository = casinoRepository;
            _telemetryClient = telemetryClient;
        }

        [HttpGet("v1/accounts/{playerId}/session"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> GetSession(string playerId, [FromQuery] string gameId)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            var result = await _casinoRepository.GetSession(new GameSessionRequest
            {
                WalletSession = walletSession,
                PassKey = passKey
            });

            var properties = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "response", JsonSerializer.Serialize(result.Item2) }
                            };

            _telemetryClient.TrackEvent("e:v1/accounts/{playerId}/session", properties);

            switch (result.Item1)
            {
                case Enums.SessionCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.Item1.ToString() });
            }
        }

        [HttpGet("v1/accounts/{playerId}/balance"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> GetBalance(string playerId, [FromQuery] string gameId)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            var result = await _casinoRepository.GetBalance(new BalanceRequest
            {
                WalletSession = walletSession,
                PassKey = passKey
            });

            var properties = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "response", JsonSerializer.Serialize(result.Item2) }
                            };

            _telemetryClient.TrackEvent("e:v1/accounts/{playerId}/balance", properties);

            switch (result.Item1)
            {
                case Enums.BalanceCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.Item1.ToString() });
            }
        }

        [HttpPost("v1/transactions"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> ProcessTransaction([FromBody] TransactionRequest request)
        {//
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            var initial = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) }
                            };

            _telemetryClient.TrackEvent("e:v1/transactions/initial", initial);

            if (request.BonusType == null)
                request.BonusType = string.Empty;

            if (request.BonusPromoCode == null)
                request.BonusPromoCode = string.Empty;

            if (request.TableId == null)
                request.TableId = string.Empty;

            if (request.JpContributions == null)
                request.JpContributions =  new JackpotContribution[] { };

            if (request.ClientRoundId == null)
                request.ClientRoundId = string.Empty;

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

                var properties = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) },
                                { "response", JsonSerializer.Serialize(withdrawResponse.Item2) }
                            };

                _telemetryClient.TrackEvent("e:v1/transactions", properties);

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

                var properties = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) },
                                { "response", JsonSerializer.Serialize(depositResponse.Item2) }
                            };

                _telemetryClient.TrackEvent("e:v1/transactions", properties);

                switch (depositResponse.Item1)
                {
                    case Enums.DepositCode.OK:
                        return Ok(depositResponse.Item2);
                    default:
                        return BadRequest(new { code = depositResponse.Item1.ToString() });
                }
            }
        }

        [HttpPost("v1/transactions/rollback"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> ProcessRollback([FromBody] RollbackRequest request)
        {
            var walletSession = HttpContext.Request.Headers["Wallet-Session"].FirstOrDefault();
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            request.WalletSession = walletSession;
            request.PassKey = passKey;

            var result = await _casinoRepository.Rollback(request);

            var properties = new Dictionary<string, string>
                            {
                                { "Wallet-Session", walletSession },
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) },
                                { "response", JsonSerializer.Serialize(result.Item2) }
                            };

            _telemetryClient.TrackEvent("e:v1/transactions/rollback", properties);

            switch (result.Item1)
            {
                case Enums.RollbackCode.OK:
                    return Ok(result.Item2);
                default:
                    return BadRequest(new { code = result.ToString() });
            }
        }

        [HttpPost("v1/bonus/status"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> ProcessBonusStatus([FromBody] BonusStatusRequest request)
        {
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            request.PassKey = passKey;

            var result = await _casinoRepository.ProcessBonusStatus(request);

            var properties = new Dictionary<string, string>
                            {
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/bonus/status", properties);

            switch (result)
            {
                case Enums.BonusStatusCode.OK:
                    return NoContent();
                default:
                    return BadRequest(new { code = result.ToString() });
            }
        }

        [HttpPost("v1/bonus/rewards"), ServiceFilter(typeof(PassKeyFilter))]
        public async Task<IActionResult> ProcessBonusReward([FromBody] BonusRewardRequest request)
        {
            var passKey = HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();
            var contentType = HttpContext.Request.Headers["Content-Type"].FirstOrDefault();

            request.PassKey = passKey;
            request.ContentType = contentType;

            var result = await _casinoRepository.ProcessBonusReward(request);

            var properties = new Dictionary<string, string>
                            {
                                { "Pass-Key", passKey },
                                { "request", JsonSerializer.Serialize(request) },
                                { "response", JsonSerializer.Serialize(result) }
                            };

            _telemetryClient.TrackEvent("e:v1/bonus/rewards", properties);

            switch (result.Item1)
            {
                case Enums.BonusRewardCode.OK:
                    return Created("", result.Item2);
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
                        Message = result.Item1.ToString()
                    });
            }
        }

        [HttpPost("v1/bet/history")]
        public async Task<IActionResult> GetBetHistory([FromBody] BetHistoryRequestDTO request)
        {
            //
            var result = await _casinoRepository.GetBetHistory(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}

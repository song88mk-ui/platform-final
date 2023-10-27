using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Betting.API.Repository;
using Platform.Shared.Models;
using System.Threading.Tasks;

namespace Platform.Betting.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class BetController : ControllerBase
    {
        private readonly IBettingRepository _bettingRepository;

        public BetController(IBettingRepository bettingRepository)
        {
            _bettingRepository = bettingRepository;
        }

        [HttpPost("v1/bet/placement")]
        public async Task<IActionResult> PlaceBet([FromBody] BetRequestDTO request)
        {
            var result = await _bettingRepository.PlaceBet(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/maxstake")]
        public async Task<IActionResult> GetMaxStake([FromBody] BetRequestDTO request)
        {
            var result = await _bettingRepository.GetMaxStake(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/history")]
        public async Task<IActionResult> GetBetHistory([FromBody] BetHistoryRequestDTO request)
        {
            var result = await _bettingRepository.GetBetHistory(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/history/settled")]
        public async Task<IActionResult> GetBetHistorySettled([FromBody] BetHistorySettledRequestDTO request)
        {
            var result = await _bettingRepository.GetBetHistorySettled(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/history/running")]
        public async Task<IActionResult> GetBetHistoryRunning([FromBody] BetHistoryRunningRequestDTO request)
        {
            var result = await _bettingRepository.GetBetHistoryRunning(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/config")]
        public async Task<IActionResult> GetBetConfig([FromBody] BetConfigRequestDTO request)
        {
            var result = await _bettingRepository.GetBetConfig(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        //[HttpPost("v1/bet/config/user")]
        //public async Task<IActionResult> GetUserBetConfig([FromBody] UserBetConfigDetailsRequestDTO request)
        //{
        //    var result = await _bettingRepository.GetUserBetConfig(request);

        //    switch (result.StatusCode)
        //    {
        //        case Shared.Enums.StatusCode.Success:
        //            return Ok(result);
        //        default:
        //            return BadRequest(result);
        //    }
        //}

        [HttpPost("v1/bet/cancel")]
        public async Task<IActionResult> CancelBets([FromBody] BetModifyRequestDTO request)
        {
            var result = await _bettingRepository.CancelBets(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/manualsettlement")]
        public async Task<IActionResult> ManualSettlement([FromBody] ManualBetSettlementRequest request)
        {
            var result = await _bettingRepository.ManualSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/details")]
        public async Task<IActionResult> GetManualBetDetails([FromBody] ManualBetDetailsRequest request)
        {
            var result = await _bettingRepository.GetManualBetDetails(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/selections")]
        public async Task<IActionResult> GetBetSelections(BetSelectionDetailsRequestDTO request)
        {
            var result = await _bettingRepository.GetBetSelections(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("v1/bet/resendsettlement")]
        public async Task<IActionResult> ResendSettlement([FromBody] ResendSettlementMessageRequestDTO request)
        {
            var result = await _bettingRepository.ResendSettlement(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/cashout")]
        public async Task<IActionResult> CashoutBet([FromBody] CashoutBetRequestDTO request)
        {
            var result = await _bettingRepository.CashOutBet(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("v1/bet/cashout/state")]
        public async Task<IActionResult> GetCashOutState([FromBody] CashoutStateRequestDTO request)
        {
            var result = await _bettingRepository.GetCashOut(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        //
        [HttpPost("v1/bet/status")]
        public async Task<IActionResult> GetBetStatus([FromBody] BetStatusRequestDTO request)
        {
            var result = await _bettingRepository.GetBetStatus(request);

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
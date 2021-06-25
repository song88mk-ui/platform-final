using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Betting.API.Repository
{
    public class CashoutRepository : ICashoutRepository
    {
        #region Fields

        private readonly BettingContext _bettingContext;
        private readonly IConfiguration _configuration;
        private readonly IMessageBroker _messageBroker;

        #endregion

        #region Ctor

        public CashoutRepository(BettingContext bettingContext, IConfiguration configuration, IMessageBroker messageBroker)
        {
            _bettingContext = bettingContext;
            _configuration = configuration;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }

        #endregion

        #region Utilities

        private async Task<BaseResponseDTO<bool>> CashoutBetWithMTS(CashoutBetRequestDTO request)
        {
            //var bet = _bettingContext.Bet.FirstOrDefault(c => c.BetId == request.BetId);

            //var betSelectionIds = request.Selections.Select(c => c.BetSelectionId).ToList();
            //var betSelections = _bettingContext.BetSelection.Where(c => c.BetId == request.BetId && betSelectionIds.Contains(c.BetSelectionId)).ToList();

            //if (bet.BetStatusId != (int)BetStatus.Running)
            //{
            //    return new BaseResponseDTO<bool>
            //    {
            //        StatusCode = StatusCode.Bet_Status_Must_Be_Running,
            //        Message = StatusCode.Bet_Status_Must_Be_Running.ToString(),
            //        Result = false
            //    };
            //}

            //var potentialPayout = bet.Stake * bet.Odds;

            //if (request.Cashout > potentialPayout)
            //{
            //    return new BaseResponseDTO<bool>
            //    {
            //        StatusCode = StatusCode.Cashout_Must_Be_Less_Than_Potential_Payout,
            //        Message = StatusCode.Cashout_Must_Be_Less_Than_Potential_Payout.ToString(),
            //        Result = false
            //    };
            //}

            //// parlay validations
            //if (betSelections.Count > 1)
            //{
            //    // for Parlay, Cashout can only be offered if Cashout Value > Stake. 
            //    // this is to minimize showing of the gap or large margin between stake and cashout offer.
            //    if (request.Cashout < bet.Stake)
            //    {
            //        return new BaseResponseDTO<bool>
            //        {
            //            StatusCode = StatusCode.Cashout_Must_Be_Greater_Than_Stake,
            //            Message = StatusCode.Cashout_Must_Be_Greater_Than_Stake.ToString(),
            //            Result = false
            //        };
            //    }
            //}

            //_messageBroker.Publish(new OnCashoutBetSettlement
            //{
            //    Bet = bet,
            //    BetSelections = betSelections,
            //}, "OnCashoutBetSettlement", "r.bet.settlement", Guid.NewGuid().ToString().ToUpper());

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
        }

        private async Task<BaseResponseDTO<bool>> CashoutBetWithoutMTS(CashoutBetRequestDTO request)
        {
            //var bet = _bettingContext.Bet.FirstOrDefault(c => c.BetId == request.BetId);

            //var betSelectionIds = request.Selections.Select(c => c.BetSelectionId).ToList();
            //var betSelections = _bettingContext.BetSelection.Where(c => c.BetId == request.BetId).ToList();
            //var selectedBetSelections = betSelections.Where(c => betSelectionIds.Contains(c.BetSelectionId)).ToList();

            //if (bet.BetStatusId != (int)BetStatus.Running)
            //{
            //    return new BaseResponseDTO<bool>
            //    {
            //        StatusCode = StatusCode.Bet_Status_Must_Be_Running,
            //        Message = StatusCode.Bet_Status_Must_Be_Running.ToString(),
            //        Result = false
            //    };
            //}

            //var potentialPayout = bet.Stake * bet.Odds;

            //if (request.Cashout > potentialPayout)
            //{
            //    return new BaseResponseDTO<bool>
            //    {
            //        StatusCode = StatusCode.Cashout_Must_Be_Less_Than_Potential_Payout,
            //        Message = StatusCode.Cashout_Must_Be_Less_Than_Potential_Payout.ToString(),
            //        Result = false
            //    };
            //}

            //// parlay validations
            //if (betSelections.Count > 1)
            //{
            //    if (request.Cashout < bet.Stake)
            //    {
            //        return new BaseResponseDTO<bool>
            //        {
            //            StatusCode = StatusCode.Cashout_Must_Be_Greater_Than_Stake,
            //            Message = StatusCode.Cashout_Must_Be_Greater_Than_Stake.ToString(),
            //            Result = false
            //        };
            //    }
            //}

            //_messageBroker.Publish(new OnCashoutBetSettlement
            //{
            //    Bet = bet,
            //    BetSelections = betSelections,
            //}, "OnCashoutBetSettlement", "r.bet.settlement", Guid.NewGuid().ToString().ToUpper());

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
            };
        }

        #endregion

        #region Methods

        public async Task<BaseResponseDTO<bool>> CashoutBet(CashoutBetRequestDTO request)
        {
            return new BaseResponseDTO<bool>();
            //var betSelectionIds = request.Selections.Select(s => s.BetSelectionId).ToList();
            //var sportIds = _bettingContext.BetSelection
            //    .Where(c => c.BetId == request.BetId && betSelectionIds.Contains(c.BetSelectionId))
            //    .Select(s => s.SportId)
            //    .ToList();

            //if (sportIds.Any(c => c > 1000))
            //    return await CashoutBetWithoutMTS(request);
            //else
            //{
            //    if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) == true)
            //        return await CashoutBetWithMTS(request);
            //    else
            //        return await CashoutBetWithoutMTS(request);
            //}
        }

        #endregion
    }
}

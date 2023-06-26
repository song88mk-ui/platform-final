using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.AgentReports.API.Models.Request;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Data.Reports;
using Platform.Shared.Data.Reports.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.AgentReports.API.Repository
{
    public class PaymentsRepository : IPaymentsRepository
    {
        #region Fields

        private readonly AgentReportsContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWalletRepository _walletRepository;

        private readonly bool _isFullBalanceTransferEnabled;

        #endregion

        #region Ctor

        public PaymentsRepository(AgentReportsContext context, IConfiguration configuration, IWalletRepository walletRepository)
        {
            _context = context;
            _configuration = configuration;
            _walletRepository = walletRepository;

            _isFullBalanceTransferEnabled = Convert.ToBoolean(_configuration["Custom:IsFullBalanceTransferEnabled"]);
        }

        #endregion

        #region Methods

        public async Task<BaseResponseDTO<PaymentTransferListResponse>> GetTransfers(PaymentTransferListRequest request)
        {
            return _isFullBalanceTransferEnabled ? await GetTransfersWithoutYesterdayBalance(request) : await GetTransfersWithYesterdayBalance(request);
        }

        public async Task<BaseResponseDTO<PaymentTransferResponse>> Transfer(PaymentTransferRequest request)
        {
            return _isFullBalanceTransferEnabled ? await TransferWithoutYesterdayBalance(request) : await TransferWithYesterdayBalance(request);

        }

        public async Task<BaseResponseDTO<long>> GGRSettlement(GGRSettlementRequest request)
        {
            var transactionDate = DateTime.UtcNow;
            var unsettledRevenues = _context.SettlementHistory.Where(s => s.UserId == request.UserId && s.IsSettled == false).ToList();

            if (request.SettlementAmount != unsettledRevenues.Sum(u => u.Balance))
            {
                return new BaseResponseDTO<long>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace("_", " "),
                    Result = request.UserId
                };
            }

            foreach (var unsettledRevenue in unsettledRevenues)
            {
                var unsettled = _context.SettlementHistory.FirstOrDefault(s => s.Id == unsettledRevenue.Id);

                unsettled.IsSettled = true;
                unsettled.DateSettled = transactionDate;
                unsettled.AmountSettled = request.SettlementAmount;
                unsettled.SettledByUserId = request.SettledByUserId;
                unsettled.SettledBy = request.SettledByUsername;

                _context.Update(unsettled);
            }

            _context.SaveChanges();

            return new BaseResponseDTO<long>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace("_", " "),
                Result = request.UserId
            };
        }

        #endregion

        #region Utilities

        private async Task<BaseResponseDTO<PaymentTransferListResponse>> GetTransfersWithYesterdayBalance(PaymentTransferListRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var transferResponse = new List<PaymentTransferListResponse.Detail>();

            var query = _context.RunningBalance.AsNoTracking().Where(c => c.ParentUserId == request.ParentAgentId && c.UserTypeId == request.UserTypeId);

            if (!string.IsNullOrEmpty(request.Username))
                query = query.Where(c => c.Username.Contains(request.Username));

            if (!string.IsNullOrEmpty(request.CurrencyCode))
                query = query.Where(c => c.CurrencyCode == request.CurrencyCode.Trim());

            var runningBalance = query.ToList();
            var rowCount = runningBalance.Count(); 

            if (rowCount == 0)
            {
                return new BaseResponseDTO<PaymentTransferListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace("_", " "),
                    Result = new PaymentTransferListResponse()
                };
            }

            var fullTransactionTypes = new List<int> { (int)TransactionType.CreditFullPaymentTransfer, (int)TransactionType.DebitFullPaymentTransfer };
            var partialTransactionTypes = new List<int> { (int)TransactionType.CreditPartialPaymentTransfer, (int)TransactionType.DebitPartialPaymentTransfer };

            var outstandingBets = _context.Bet.Where(c => c.ParentUserId == request.ParentAgentId && c.StatusId == (int)StatusType.Running).AsNoTracking();

            foreach (var item in runningBalance)
            {
                var minDateTime = DateTime.MinValue;

                var lastFullTransferDate = _context.TransferHistory.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.UserId == item.UserId && fullTransactionTypes.Contains(c.TransactionTypeId))
                    .OrderByDescending(o => o.TransactionDateOffset)
                    .Select(s => s.TransactionDateOffset)
                    .FirstOrDefault();

                if (lastFullTransferDate == null) 
                    lastFullTransferDate = minDateTime;

                var partialTransfers = _context.TransferHistory.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.UserId == item.UserId && partialTransactionTypes.Contains(c.TransactionTypeId) && c.TransactionDateOffset > lastFullTransferDate)
                    .Sum(s => s.Amount);

                var yesterdayWinLoss = _context.WinLoss.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.DirectUserId == request.ParentAgentId && c.IndirectUserId == item.UserId && c.SettledDateOffset > lastFullTransferDate && c.SettledDateOffset.Date <= DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                    .Sum(s => item.UserTypeId == (int)UserType.Player ? s.MemberWLC : (s.AgentWLC + s.UplineWLC) * -1);

                var totalYesterdayBalance = yesterdayWinLoss == 0 ? 0 : yesterdayWinLoss - partialTransfers;

                var todayCashBalance = request.UserTypeId == (int)UserType.Agent ? ((item.CashBalance * -1) - totalYesterdayBalance) : ((item.CashBalance * -1) - totalYesterdayBalance) * -1;
                var totalOutstanding = 0m;

                if (request.UserTypeId == (int)UserType.Agent)
                {
                    var agentBets = _context.Bet.Where(c => (outstandingBets.Select(s => s.BetId).Contains(c.BetId)) && c.ParentUserId == item.UserId).ToList();

                    foreach (var agentBet in agentBets)
                    {
                        var obet = outstandingBets.FirstOrDefault(c => c.BetId == agentBet.BetId);
                        totalOutstanding += (obet.Stake * obet.PositionTaking);
                    }
                }
                else
                {
                    totalOutstanding = item.OutstandingBalance;
                }

                //GET PREV DAY CASH BALANCE FOR TRANSFERAMOUNT
                var transferAmount = item.TrueCashBalance;
                if (item.UserTypeId == (int)UserType.Player)
                {
                    var wlcToday = _context.WinLoss.Where(c => c.ParentUserId == request.ParentAgentId && c.IndirectUserId == item.UserId && c.SettledDateOffset.Date == DateTime.UtcNow.AddHours(-4).Date);

                    if (wlcToday.Any() && item.TrueCashBalance != 0)
                    {
                        transferAmount = (item.CashBalance + wlcToday.Sum(s => s.MemberWLC));// - totalOutstanding;
                    }
                }
                else
                {
                    transferAmount = totalYesterdayBalance;
                }

                if (lastFullTransferDate.Date == DateTime.UtcNow.AddHours(-4).Date)
                    transferAmount = 0;

                transferResponse.Add(new PaymentTransferListResponse.Detail
                {
                    UserId = item.UserId,
                    Username = item.Username,
                    UserTypeId = item.UserTypeId,
                    OutstandingBet = totalOutstanding,
                    YesterdayCashBalance = totalYesterdayBalance,
                    TodayCashBalance = item.CashBalance == 0 ? 0 : todayCashBalance,
                    CashBalance = item.CashBalance == 0 ? 0 : item.CashBalance * -1,
                    TransferAmount = transferAmount
                });
            }

            return new BaseResponseDTO<PaymentTransferListResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new PaymentTransferListResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = rowCount,
                    TotalYesterdayCashBalance = transferResponse.Sum(s => s.YesterdayCashBalance),
                    TotalTodayCashBalance = transferResponse.Sum(s => s.TodayCashBalance),
                    TotalCashBalance = transferResponse.Sum(s => s.CashBalance),
                    TotalOutstandingBet = transferResponse.Sum(s => s.OutstandingBet),
                    TotalTransferAmount = transferResponse.Sum(s => s.TransferAmount),
                    Transfers = transferResponse.Skip(skip).Take(request.PageSize).ToList(),
                    UserIds = string.Join(",", transferResponse.Select(s => s.UserId.ToString()))
                }
            };
        }

        private async Task<BaseResponseDTO<PaymentTransferListResponse>> GetTransfersWithoutYesterdayBalance(PaymentTransferListRequest request)
        {
            var skip = (request.Page - 1) * request.PageSize;
            var transferResponse = new List<PaymentTransferListResponse.Detail>();

            var query = _context.RunningBalance.AsNoTracking().Where(c => c.ParentUserId == request.ParentAgentId && c.UserTypeId == request.UserTypeId && c.IsCashPlayer == request.IsCashMarket);

            if (!string.IsNullOrEmpty(request.Username))
                query = query.Where(c => c.Username.Contains(request.Username));

            if (!string.IsNullOrEmpty(request.CurrencyCode))
                query = query.Where(c => c.CurrencyCode == request.CurrencyCode.Trim());

            var runningBalance = query.OrderBy(o => o.UserId).ToList();
            var rowCount = runningBalance.Count();

            if (rowCount == 0)
            {
                return new BaseResponseDTO<PaymentTransferListResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace("_", " "),
                    Result = new PaymentTransferListResponse()
                };
            }

            var fullTransactionTypes = new List<int> { (int)TransactionType.CreditFullPaymentTransfer, (int)TransactionType.DebitFullPaymentTransfer };
            var partialTransactionTypes = new List<int> { (int)TransactionType.CreditPartialPaymentTransfer, (int)TransactionType.DebitPartialPaymentTransfer };

            var outstandingBets = _context.Bet.AsNoTracking().Where(c => c.ParentUserId == request.ParentAgentId && c.StatusId == (int)StatusType.Running).ToList();

            foreach (var item in runningBalance)
            {
                var minDateTime = DateTime.MinValue;

                var lastFullTransferDate = _context.TransferHistory.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.UserId == item.UserId && fullTransactionTypes.Contains(c.TransactionTypeId))
                    .OrderByDescending(o => o.TransactionDateOffset)
                    .Select(s => s.TransactionDateOffset)
                    .FirstOrDefault();

                if (lastFullTransferDate == null)
                    lastFullTransferDate = minDateTime;

                var partialTransfers = _context.TransferHistory.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.UserId == item.UserId && partialTransactionTypes.Contains(c.TransactionTypeId) && c.TransactionDateOffset > lastFullTransferDate)
                    .Sum(s => s.Amount);

                var yesterdayWinLoss = _context.WinLoss.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.DirectUserId == request.ParentAgentId && c.IndirectUserId == item.UserId && c.SettledDateOffset > lastFullTransferDate && c.SettledDateOffset.Date <= DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                    .Select(s => new
                    {
                        s.MemberWLC,
                        s.AgentWLC,
                        s.UplineWLC
                    })
                    .ToList()
                    .Sum(s => item.UserTypeId == (int)UserType.Player ? s.MemberWLC : (s.AgentWLC + s.UplineWLC) * -1);
                //Casino Winloss
                yesterdayWinLoss += _context.CasinoWinLoss.AsNoTracking()
                    .Where(c => c.ParentUserId == request.ParentAgentId && c.DirectUserId == request.ParentAgentId && c.IndirectUserId == item.UserId && c.SettledDate > lastFullTransferDate && c.SettledDate.Date <= DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                    .Select(s => new
                    {
                        s.MemberWLC,
                        s.AgentWLC,
                        s.UplineWLC
                    })
                    .ToList()
                    .Sum(s => item.UserTypeId == (int)UserType.Player ? s.MemberWLC : (s.AgentWLC + s.UplineWLC) * -1);

                var totalYesterdayBalance = yesterdayWinLoss == 0 ? 0 : yesterdayWinLoss - partialTransfers;
                var todayCashBalance = request.UserTypeId == (int)UserType.Agent ? ((item.CashBalance * -1) - totalYesterdayBalance) : ((item.CashBalance * -1) - totalYesterdayBalance) * -1;
                var totalOutstanding = 0m;

                if (request.UserTypeId == (int)UserType.Agent)
                {
                    var agentBets = _context.Bet.AsNoTracking().Where(c => (outstandingBets.Select(s => s.BetId).Contains(c.BetId)) && c.ParentUserId == item.UserId)
                        .Select(s => new
                        {
                            s.BetId
                        }).ToList();

                    foreach (var agentBet in agentBets)
                    {
                        var obet = outstandingBets.FirstOrDefault(c => c.BetId == agentBet.BetId);
                        totalOutstanding += obet.Stake * obet.PositionTaking;
                    }
                }
                else
                {
                    totalOutstanding = item.OutstandingBalance;
                }

                //GET PREV DAY CASH BALANCE FOR TRANSFERAMOUNT
                var transferAmount = item.TrueCashBalance;

                if (item.UserTypeId == (int)UserType.Player)
                {
                    //YESTERDAY BALANCE FOR MEMBER
                    if (yesterdayWinLoss != 0 && (yesterdayWinLoss > 0 && partialTransfers < 0 || yesterdayWinLoss < 0 && partialTransfers > 0))
                    {
                        var partialTransferAbsolute = _context.TransferHistory.AsNoTracking().Where(c => c.ParentUserId == request.ParentAgentId && c.UserId == item.UserId && partialTransactionTypes.Contains(c.TransactionTypeId) && c.TransactionDateOffset > lastFullTransferDate).Sum(p => Math.Abs(p.Amount));

                        if ((Math.Abs(yesterdayWinLoss) - partialTransferAbsolute) < 0)
                        {
                            totalYesterdayBalance = 0;
                        }
                        else
                        {
                            totalYesterdayBalance = Math.Abs(yesterdayWinLoss) - Math.Abs(partialTransferAbsolute);

                            if (yesterdayWinLoss < 0)
                                totalYesterdayBalance = totalYesterdayBalance * -1;
                        }
                    }
                }
                else
                {
                    transferAmount = totalYesterdayBalance;
                }

                //CASH PLAYER ACCUMULATE BALANCE
                if (item.UserTypeId == (int)UserType.Player && item.IsCashPlayer)
                {
                    //GET ACCUM BALANCE
                    var memberWLC = _context.WinLoss.AsNoTracking()
                        .Where(c => c.ParentUserId == request.ParentAgentId && c.DirectUserId == request.ParentAgentId && c.IndirectUserId == item.UserId)
                        .Select(s => s.MemberWLC)
                        .Sum();

                    //memberWLC=memberWLC == 0 ? 0 : (memberWLC * -1);
                    var memberWithdrawal = _context.TransferHistory.AsNoTracking()
                        .Where(c => c.UserId == item.UserId && c.TransactionTypeId == (int)TransactionType.Withdrawal)
                        .Select(s => s.Amount)
                        .Sum();

                    var accumBalance = 0m;

                    if (memberWLC < 0)
                    {
                        accumBalance = memberWLC + memberWithdrawal;
                    }
                    else
                    {
                        accumBalance = memberWLC - memberWithdrawal;
                    }

                    item.CashBalance = memberWithdrawal == 0 ? accumBalance : (memberWithdrawal > accumBalance ? 0 : (accumBalance));

                    //GET YESTERDAY BALANCE
                    var memberYesterdayWLC = _context.WinLoss.AsNoTracking()
                        .Where(c => c.ParentUserId == request.ParentAgentId && c.DirectUserId == request.ParentAgentId && c.IndirectUserId == item.UserId && c.SettledDateOffset.Date <= DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                        .Select(s => s.MemberWLC)
                        .ToList()
                        .Sum();

                    //memberYesterdayWLC= memberYesterdayWLC==0? 0 : (memberYesterdayWLC * -1);

                    var memberYesterdayWithdrawal = _context.TransferHistory.AsNoTracking()
                        .Where(c => c.UserId == item.UserId && c.TransactionTypeId == (int)TransactionType.Withdrawal && c.TransactionDateOffset.Date <= DateTime.UtcNow.AddHours(-4).AddDays(-1).Date)
                        .Select(s => s.Amount)
                        .Sum();

                    var accumYesterdayBalance = 0m;

                    if (memberYesterdayWLC < 0)
                    {
                        accumYesterdayBalance = memberYesterdayWLC + memberYesterdayWithdrawal;
                    }
                    else
                    {
                        accumYesterdayBalance = memberYesterdayWLC - memberYesterdayWithdrawal;
                    }

                    totalYesterdayBalance = memberYesterdayWithdrawal == 0 ? accumYesterdayBalance : (memberYesterdayWithdrawal > accumYesterdayBalance ? 0 : accumYesterdayBalance);
                }

                transferResponse.Add(new PaymentTransferListResponse.Detail
                {
                    UserId = item.UserId,
                    Username = item.Username,
                    UserTypeId = item.UserTypeId,
                    OutstandingBet = totalOutstanding,
                    YesterdayCashBalance = totalYesterdayBalance,
                    TodayCashBalance = item.CashBalance == 0 ? 0 : todayCashBalance,
                    CashBalance = item.CashBalance == 0 ? 0 : (item.IsCashPlayer ? item.CashBalance : (item.CashBalance * -1)),
                    TransferAmount = transferAmount
                });
            }

            return new BaseResponseDTO<PaymentTransferListResponse>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new PaymentTransferListResponse
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling((double)rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = rowCount,
                    TotalYesterdayCashBalance = transferResponse.Sum(s => s.YesterdayCashBalance),
                    TotalTodayCashBalance = transferResponse.Sum(s => s.TodayCashBalance),
                    TotalCashBalance = transferResponse.Sum(s => s.CashBalance),
                    TotalOutstandingBet = transferResponse.Sum(s => s.OutstandingBet),
                    TotalTransferAmount = transferResponse.Sum(s => s.TransferAmount),
                    Transfers = transferResponse.Skip(skip).Take(request.PageSize).ToList(),
                    UserIds = string.Join(",", transferResponse.Select(s => s.UserId.ToString()))
                }
            };
        }

        private async Task<BaseResponseDTO<PaymentTransferResponse>> TransferWithoutYesterdayBalance(PaymentTransferRequest request)
        {
            try
            {
                var result = new PaymentTransferResponse();
                var balance = _context.RunningBalance.FirstOrDefault(c => c.ParentUserId == request.ParentAgentId && c.UserId == request.UserId);

                if (balance == null)
                {
                    return new BaseResponseDTO<PaymentTransferResponse>
                    {
                        StatusCode = StatusCode.No_Record_Found,
                        Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                        Result = new PaymentTransferResponse()
                    };
                }

                var truncatedTrueCashBalance = Math.Truncate(100 * balance.TrueCashBalance) / 100;

                if (truncatedTrueCashBalance.Equals(request.TransferAmount))
                {
                    request.TransferAmount = balance.TrueCashBalance;
                }

                if (!request.IsCashMarket)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var cashBalanceTracker = _context.CashBalanceTracker.FirstOrDefault(c => c.UserId == request.UserId && c.StatusId == (int)StatusType.Running);

                        if (request.TransferAmount != 0)
                        {
                            request.TransferAmount = balance.UserTypeId == (int)UserType.Agent ? request.TransferAmount : request.TransferAmount * -1;
                        }

                        var preCashBalance = balance.CashBalance;
                        var preTrueCashBalance = balance.TrueCashBalance;

                        if ((balance.TrueCashBalance > 0 && request.TransferAmount > 0) || (balance.TrueCashBalance < 0 && request.TransferAmount < 0))
                        {
                            scope.Complete();

                            return new BaseResponseDTO<PaymentTransferResponse>
                            {
                                StatusCode = StatusCode.No_Record_Found,
                                Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                                Result = new PaymentTransferResponse()
                            };
                        }

                        if ((Math.Abs(request.TransferAmount) > Math.Abs(balance.TrueCashBalance)) && balance.UserTypeId == (int)UserType.Player)
                        {
                            scope.Complete();

                            return new BaseResponseDTO<PaymentTransferResponse>
                            {
                                StatusCode = StatusCode.No_Record_Found,
                                Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                                Result = new PaymentTransferResponse()
                            };
                        }

                        result = new PaymentTransferResponse
                        {
                            UserId = request.UserId,
                            TransferAmount = request.TransferAmount,
                            IsFullTransfer = ((balance.TrueCashBalance + request.TransferAmount) == 0 && cashBalanceTracker == null) ? true : false,
                            ReferenceId = request.ReferenceId.ToString()
                        };

                        var transactionType = new TransactionType();

                        if (!result.IsFullTransfer && (balance.TrueCashBalance < 0))
                            transactionType = TransactionType.DebitPartialPaymentTransfer;

                        if (!result.IsFullTransfer && (balance.TrueCashBalance > 0))
                            transactionType = TransactionType.CreditPartialPaymentTransfer;

                        if (result.IsFullTransfer && (balance.TrueCashBalance < 0))
                            transactionType = TransactionType.DebitFullPaymentTransfer;

                        if (result.IsFullTransfer && (balance.TrueCashBalance > 0))
                            transactionType = TransactionType.CreditFullPaymentTransfer;

                        var types = new TransactionType[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.CreditPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer, TransactionType.CreditFullPaymentTransfer };

                        if (!types.Contains(transactionType))
                        {
                            scope.Complete();

                            return new BaseResponseDTO<PaymentTransferResponse>
                            {
                                StatusCode = StatusCode.No_Record_Found,
                                Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                                Result = new PaymentTransferResponse()
                            };
                        }

                        #region Wallet

                        var amount = Math.Abs(request.TransferAmount);

                        // wallet repository here
                        var creditTypes = new TransactionType[] { TransactionType.CreditPartialPaymentTransfer, TransactionType.CreditFullPaymentTransfer };

                        if (creditTypes.Contains(transactionType))
                        {
                            var walletResponse = _walletRepository.Credit(new CreditOrDebitRequestDTO
                            {
                                ClientId = Guid.Parse(request.ClientId),
                                UserId = request.UserId.ToString(),
                                Amount = amount,
                                CurrencyCode = balance.CurrencyCode,
                                ReferenceId = Guid.NewGuid().ToString(),
                                TransactionTypeId = (int)transactionType,
                                TransactionTypeName = transactionType.ToString()
                            }).Result;

                            if (walletResponse.StatusCode != 1000)
                            {
                                scope.Complete();

                                return new BaseResponseDTO<PaymentTransferResponse>
                                {
                                    StatusCode = StatusCode.Error,
                                    Message = walletResponse.Message,
                                    Result = new PaymentTransferResponse()
                                };
                            }
                        }

                        var debitTypes = new TransactionType[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer };

                        if (debitTypes.Contains(transactionType))
                        {
                            var walletResponse = _walletRepository.Debit(new CreditOrDebitRequestDTO
                            {
                                ClientId = Guid.Parse(request.ClientId),
                                UserId = request.UserId.ToString(),
                                Amount = amount,
                                CurrencyCode = balance.CurrencyCode,
                                ReferenceId = Guid.NewGuid().ToString(),
                                TransactionTypeId = (int)transactionType,
                                TransactionTypeName = transactionType.ToString()
                            }).Result;

                            if (walletResponse.StatusCode != 1000)
                            {
                                scope.Complete();

                                return new BaseResponseDTO<PaymentTransferResponse>
                                {
                                    StatusCode = StatusCode.Error,
                                    Message = walletResponse.Message,
                                    Result = new PaymentTransferResponse()
                                };
                            }
                        }

                        #endregion

                        #region Old Code

                        //if (balance.UserTypeId == (int)UserType.Player)
                        //{
                        //    var amount = 0m;

                        //    if (result.IsFullTransfer)
                        //    {
                        //        if (balance.UserTypeId == (int)UserType.Player)
                        //        {
                        //            amount = request.OriginalCreditLimit - balance.OutstandingBalance;
                        //        }
                        //        else
                        //        {
                        //            amount = request.OriginalCreditLimit;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        amount = Math.Abs(request.TransferAmount);
                        //    }

                        //    // wallet repository here
                        //    var creditTypes = new TransactionType[] { TransactionType.CreditPartialPaymentTransfer, TransactionType.CreditFullPaymentTransfer };

                        //    if (creditTypes.Contains(transactionType))
                        //    {
                        //        var walletResponse = _walletRepository.Credit(new CreditOrDebitRequestDTO
                        //        {
                        //            ClientId = Guid.Parse(request.ClientId),
                        //            UserId = request.UserId.ToString(),
                        //            Amount = amount,
                        //            CurrencyCode = balance.CurrencyCode,
                        //            ReferenceId = Guid.NewGuid().ToString(),
                        //            TransactionTypeId = (int)transactionType,
                        //            TransactionTypeName = transactionType.ToString()
                        //        }).Result;

                        //        if (walletResponse.StatusCode != 1000)
                        //        {
                        //            scope.Complete();

                        //            return new BaseResponseDTO<PaymentTransferResponse>
                        //            {
                        //                StatusCode = StatusCode.Error,
                        //                Message = walletResponse.Message,
                        //                Result = new PaymentTransferResponse()
                        //            };
                        //        }
                        //    }

                        //    var debitTypes = new TransactionType[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer };

                        //    if (debitTypes.Contains(transactionType))
                        //    {
                        //        var walletResponse = _walletRepository.Debit(new CreditOrDebitRequestDTO
                        //        {
                        //            ClientId = Guid.Parse(request.ClientId),
                        //            UserId = request.UserId.ToString(),
                        //            Amount = amount,
                        //            CurrencyCode = balance.CurrencyCode,
                        //            ReferenceId = Guid.NewGuid().ToString(),
                        //            TransactionTypeId = (int)transactionType,
                        //            TransactionTypeName = transactionType.ToString()
                        //        }).Result;

                        //        if (walletResponse.StatusCode != 1000)
                        //        {
                        //            scope.Complete();

                        //            return new BaseResponseDTO<PaymentTransferResponse>
                        //            {
                        //                StatusCode = StatusCode.Error,
                        //                Message = walletResponse.Message,
                        //                Result = new PaymentTransferResponse()
                        //            };
                        //        }
                        //    }
                        //}

                        #endregion

                        var dateUTCNow = DateTime.UtcNow;

                        _context.TransferHistory.Add(new TransferHistory
                        {
                            ParentUserId = request.ParentAgentId,
                            UserId = request.UserId,
                            Username = balance.Username,
                            UserTypeId = balance.UserTypeId,
                            PreBalance = balance.TrueCashBalance,
                            Amount = request.TransferAmount,
                            PostBalance = (balance.TrueCashBalance + request.TransferAmount),
                            TransactionDate = dateUTCNow,
                            ReferenceId = request.ReferenceId,
                            CurrencyCode = balance.CurrencyCode,
                            IsCashPlayer = balance.IsCashPlayer,
                            TransactionTypeId = (int)transactionType,
                            TransactionTypeName = transactionType.ToString(),
                            TransactionDateOffset = dateUTCNow.AddHours(-4)
                        });

                        balance.TrueCashBalance += request.TransferAmount;
                        balance.CashBalance += request.TransferAmount;

                        var entity = _context.RunningBalance.Update(balance).Entity;

                        _context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
                        {
                            ParentUserId = entity.ParentUserId,
                            UserId = entity.UserId,
                            Username = entity.Username,
                            UserTypeId = entity.UserTypeId,
                            CurrencyCode = entity.CurrencyCode,

                            PreCashBalance = preCashBalance,
                            CashBalance = request.TransferAmount,
                            PostCashBalance = entity.CashBalance,

                            PreTrueCashBalance = preTrueCashBalance,
                            TrueCashBalance = request.TransferAmount,
                            PostTrueCashBalance = entity.TrueCashBalance,

                            PreOutstandingBalance = entity.OutstandingBalance,
                            OutstandingBalance = 0m,
                            PostOutstandingBalance = entity.OutstandingBalance,

                            IsCashPlayer = entity.IsCashPlayer,
                            ReferenceId = request.ReferenceId.ToString().ToUpper(),
                            StatusId = (int)transactionType,
                            StatusName = transactionType.ToString(),
                            TransactionDate = dateUTCNow
                        });

                        _context.SaveChanges();
                        scope.Complete();
                    }

                    return new BaseResponseDTO<PaymentTransferResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace("_", " "),
                        Result = result
                    };
                }

                // If the process has reached up to this point. Then it failed
                return new BaseResponseDTO<PaymentTransferResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace("_", " "),
                    Result = new PaymentTransferResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<PaymentTransferResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace("_", " "),
                    Result = new PaymentTransferResponse()
                };
            }
        }

        private async Task<BaseResponseDTO<PaymentTransferResponse>> TransferWithYesterdayBalance(PaymentTransferRequest request)
        {
            try
            {
                var result = new PaymentTransferResponse();
                var balance = _context.RunningBalance.FirstOrDefault(c => c.ParentUserId == request.ParentAgentId && c.UserId == request.UserId);

                if (balance == null)
                {
                    return new BaseResponseDTO<PaymentTransferResponse>
                    {
                        StatusCode = StatusCode.No_Record_Found,
                        Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                        Result = new PaymentTransferResponse()
                    };
                }

                var truncatedTrueCashBalance = Math.Truncate(100 * balance.TrueCashBalance) / 100;

                if (truncatedTrueCashBalance.Equals(request.TransferAmount))
                {
                    request.TransferAmount = balance.TrueCashBalance;
                }

                if (!request.IsCashMarket)
                {
                    var dateUTCNow = DateTime.UtcNow;
                    var wlcToday = _context.WinLoss.Where(w => w.ParentUserId == request.ParentAgentId && w.IndirectUserId == request.UserId && w.SettledDateOffset.Date == dateUTCNow.AddHours(-4).Date);

                    if (!wlcToday.Any() || balance.UserTypeId == (int)UserType.Agent)
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                        {
                            var pendingBet = _context.CashBalanceTracker.Where(c => c.UserId == request.UserId && c.StatusId == (int)StatusType.Running).FirstOrDefault();

                            if (request.TransferAmount != 0)
                                request.TransferAmount = balance.UserTypeId == (int)UserType.Agent ? request.TransferAmount : request.TransferAmount * -1;

                            var preCashBalance = balance.CashBalance;
                            var preTrueCashBalance = balance.TrueCashBalance;

                            if ((balance.TrueCashBalance > 0 && request.TransferAmount > 0) || (balance.TrueCashBalance < 0 && request.TransferAmount < 0))
                            {
                                scope.Complete();
                                return new BaseResponseDTO<PaymentTransferResponse>
                                {
                                    StatusCode = StatusCode.No_Record_Found,
                                    Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                                    Result = new PaymentTransferResponse()
                                };
                            }

                            result = new PaymentTransferResponse
                            {
                                UserId = request.UserId,
                                TransferAmount = request.TransferAmount,
                                IsFullTransfer = ((balance.TrueCashBalance + request.TransferAmount) == 0 && pendingBet == null) ? true : false,
                                ReferenceId = request.ReferenceId.ToString()
                            };

                            var transactionType = new TransactionType();
                            if (!result.IsFullTransfer && (balance.TrueCashBalance < 0))
                                transactionType = TransactionType.DebitPartialPaymentTransfer;
                            if (!result.IsFullTransfer && (balance.TrueCashBalance > 0))
                                transactionType = TransactionType.CreditPartialPaymentTransfer;
                            if (result.IsFullTransfer && (balance.TrueCashBalance < 0))
                                transactionType = TransactionType.DebitFullPaymentTransfer;
                            if (result.IsFullTransfer && (balance.TrueCashBalance > 0))
                                transactionType = TransactionType.CreditFullPaymentTransfer;

                            var types = new[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.CreditPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer, TransactionType.CreditFullPaymentTransfer };
                            if (!types.Contains(transactionType))
                            {
                                scope.Complete();
                                return new BaseResponseDTO<PaymentTransferResponse>
                                {
                                    StatusCode = StatusCode.No_Record_Found,
                                    Message = StatusCode.No_Record_Found.ToString().Replace("_", " "),
                                    Result = new PaymentTransferResponse()
                                };
                            }

                            #region Wallet

                            var amount = Math.Abs(request.TransferAmount);

                            // wallet repository here
                            var creditTypes = new TransactionType[] { TransactionType.CreditPartialPaymentTransfer, TransactionType.CreditFullPaymentTransfer };

                            if (creditTypes.Contains(transactionType))
                            {
                                var walletResponse = _walletRepository.Credit(new CreditOrDebitRequestDTO
                                {
                                    ClientId = Guid.Parse(request.ClientId),
                                    UserId = request.UserId.ToString(),
                                    Amount = amount,
                                    CurrencyCode = balance.CurrencyCode,
                                    ReferenceId = Guid.NewGuid().ToString(),
                                    TransactionTypeId = (int)transactionType,
                                    TransactionTypeName = transactionType.ToString()
                                }).Result;

                                if (walletResponse.StatusCode != 1000)
                                {
                                    scope.Complete();

                                    return new BaseResponseDTO<PaymentTransferResponse>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = walletResponse.Message,
                                        Result = new PaymentTransferResponse()
                                    };
                                }
                            }

                            var debitTypes = new TransactionType[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer };

                            if (debitTypes.Contains(transactionType))
                            {
                                var walletResponse = _walletRepository.Debit(new CreditOrDebitRequestDTO
                                {
                                    ClientId = Guid.Parse(request.ClientId),
                                    UserId = request.UserId.ToString(),
                                    Amount = amount,
                                    CurrencyCode = balance.CurrencyCode,
                                    ReferenceId = Guid.NewGuid().ToString(),
                                    TransactionTypeId = (int)transactionType,
                                    TransactionTypeName = transactionType.ToString()
                                }).Result;

                                if (walletResponse.StatusCode != 1000)
                                {
                                    scope.Complete();

                                    return new BaseResponseDTO<PaymentTransferResponse>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = walletResponse.Message,
                                        Result = new PaymentTransferResponse()
                                    };
                                }
                            }

                            #endregion

                            #region Old Code

                            //if (balance.UserTypeId == (int)UserType.Player)
                            //{
                            //    var walletUrl = $"{_walletUrl}transfer/payment";
                            //    var walletClient = _httpClient.CreateClient();

                            //    var amount = 0m;
                            //    if (result.IsFullTransfer)
                            //    {
                            //        if (balance.UserTypeId == (int)UserType.Player)
                            //        {
                            //            amount = originalCreditLimit - balance.OutstandingBalance;
                            //        }
                            //        else
                            //        {
                            //            amount = originalCreditLimit;
                            //        }
                            //    }
                            //    else
                            //    {
                            //        amount = Math.Abs(transferAmount);
                            //    }

                            //    var payload = new
                            //    {
                            //        UserId = userId,
                            //        Amount = amount,
                            //        ReferenceId = referenceId.ToString().ToUpper(),
                            //        TransactionType = transactionType,
                            //        TransactionDate = DateTime.UtcNow
                            //    };

                            //    var walletResponse = walletClient.PostAsJsonAsync(walletUrl, payload).Result;
                            //    var walletContent = walletResponse.Content.ReadAsStringAsync();

                            //    var walletResult = JsonConvert.DeserializeObject<BaseResponse<StatusResponse>>(walletContent.Result);

                            //    if (walletResult.Result.StatusId != 1000)
                            //    {
                            //        scope.Complete();
                            //        return new BaseResponse<TransferTransactionResponse>
                            //        {
                            //            StatusCode = (int)ResultType.MissingParameters,
                            //            StatusName = ResultType.MissingParameters.ToString(),
                            //            Result = new TransferTransactionResponse()
                            //        };
                            //    }
                            //}

                            #endregion

                            _context.TransferHistory.Add(new TransferHistory
                            {
                                ParentUserId = request.ParentAgentId,
                                UserId = request.UserId,
                                Username = balance.Username,
                                UserTypeId = balance.UserTypeId,
                                PreBalance = balance.TrueCashBalance,
                                Amount = request.TransferAmount,
                                PostBalance = (balance.TrueCashBalance + request.TransferAmount),
                                TransactionDate = dateUTCNow,
                                ReferenceId = request.ReferenceId,
                                CurrencyCode = balance.CurrencyCode,
                                IsCashPlayer = balance.IsCashPlayer,
                                TransactionTypeId = (int)transactionType,
                                TransactionTypeName = transactionType.ToString(),
                                TransactionDateOffset = dateUTCNow.AddHours(-4)
                            });

                            balance.TrueCashBalance += request.TransferAmount;
                            balance.CashBalance += request.TransferAmount;

                            var entity = _context.RunningBalance.Update(balance).Entity;

                            _context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
                            {
                                ParentUserId = entity.ParentUserId,
                                UserId = entity.UserId,
                                Username = entity.Username,
                                UserTypeId = entity.UserTypeId,
                                CurrencyCode = entity.CurrencyCode,

                                PreCashBalance = preCashBalance,
                                CashBalance = request.TransferAmount,
                                PostCashBalance = entity.CashBalance,

                                PreTrueCashBalance = preTrueCashBalance,
                                TrueCashBalance = request.TransferAmount,
                                PostTrueCashBalance = entity.TrueCashBalance,

                                PreOutstandingBalance = entity.OutstandingBalance,
                                OutstandingBalance = 0m,
                                PostOutstandingBalance = entity.OutstandingBalance,

                                IsCashPlayer = entity.IsCashPlayer,
                                ReferenceId = request.ReferenceId.ToString().ToUpper(),
                                StatusId = (int)transactionType,
                                StatusName = transactionType.ToString(),
                                TransactionDate = dateUTCNow
                            });

                            _context.SaveChanges();
                            scope.Complete();
                        }

                        return new BaseResponseDTO<PaymentTransferResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString().Replace("_", " "),
                            Result = result
                        };
                    }
                    else
                    {
                        using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                        {
                            var pendingBet = _context.CashBalanceTracker.Where(c => c.UserId == request.UserId && c.StatusId == (int)StatusType.Running).FirstOrDefault();

                            if (request.TransferAmount != 0)
                                request.TransferAmount = balance.UserTypeId == (int)UserType.Agent ? request.TransferAmount : request.TransferAmount * -1;

                            var preCashBalance = balance.CashBalance;
                            var preTrueCashBalance = balance.TrueCashBalance;


                            result = new PaymentTransferResponse
                            {
                                UserId = request.UserId,
                                TransferAmount = request.TransferAmount,
                                IsFullTransfer = ((balance.TrueCashBalance + request.TransferAmount) == 0 && pendingBet == null) ? true : false,
                                ReferenceId = request.ReferenceId.ToString()
                            };

                            var transactionType = new TransactionType();
                            if (!result.IsFullTransfer && (request.TransferAmount > 0))
                                transactionType = TransactionType.DebitPartialPaymentTransfer;
                            if (!result.IsFullTransfer && (request.TransferAmount < 0))
                                transactionType = TransactionType.CreditPartialPaymentTransfer;
                            //if (result.IsFullTransfer && (balance.TrueCashBalance < 0))
                            //    transactionType = TransactionType.DebitFullPaymentTransfer;
                            //if (result.IsFullTransfer && (balance.TrueCashBalance > 0))
                            //    transactionType = TransactionType.CreditFullPaymentTransfer;

                            #region Wallet

                            var amount = Math.Abs(request.TransferAmount);

                            // wallet repository here
                            var creditTypes = new TransactionType[] { TransactionType.CreditPartialPaymentTransfer, TransactionType.CreditFullPaymentTransfer };

                            if (creditTypes.Contains(transactionType))
                            {
                                var walletResponse = _walletRepository.Credit(new CreditOrDebitRequestDTO
                                {
                                    ClientId = Guid.Parse(request.ClientId),
                                    UserId = request.UserId.ToString(),
                                    Amount = amount,
                                    CurrencyCode = balance.CurrencyCode,
                                    ReferenceId = Guid.NewGuid().ToString(),
                                    TransactionTypeId = (int)transactionType,
                                    TransactionTypeName = transactionType.ToString()
                                }).Result;

                                if (walletResponse.StatusCode != 1000)
                                {
                                    scope.Complete();

                                    return new BaseResponseDTO<PaymentTransferResponse>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = walletResponse.Message,
                                        Result = new PaymentTransferResponse()
                                    };
                                }
                            }

                            var debitTypes = new TransactionType[] { TransactionType.DebitPartialPaymentTransfer, TransactionType.DebitFullPaymentTransfer };

                            if (debitTypes.Contains(transactionType))
                            {
                                var walletResponse = _walletRepository.Debit(new CreditOrDebitRequestDTO
                                {
                                    ClientId = Guid.Parse(request.ClientId),
                                    UserId = request.UserId.ToString(),
                                    Amount = amount,
                                    CurrencyCode = balance.CurrencyCode,
                                    ReferenceId = Guid.NewGuid().ToString(),
                                    TransactionTypeId = (int)transactionType,
                                    TransactionTypeName = transactionType.ToString()
                                }).Result;

                                if (walletResponse.StatusCode != 1000)
                                {
                                    scope.Complete();

                                    return new BaseResponseDTO<PaymentTransferResponse>
                                    {
                                        StatusCode = StatusCode.Error,
                                        Message = walletResponse.Message,
                                        Result = new PaymentTransferResponse()
                                    };
                                }
                            }

                            #endregion

                            #region Old Code

                            //var walletUrl = $"{_walletUrl}transfer/payment";
                            //var walletClient = _httpClient.CreateClient();

                            //var amount = 0m;
                            //if (result.IsFullTransfer)
                            //{
                            //    if (balance.UserTypeId == (int)UserType.Player)
                            //    {
                            //        amount = originalCreditLimit - balance.OutstandingBalance;
                            //    }
                            //    else
                            //    {
                            //        amount = originalCreditLimit;
                            //    }
                            //}
                            //else
                            //{
                            //    amount = Math.Abs(transferAmount);
                            //}

                            //var payload = new
                            //{
                            //    UserId = userId,
                            //    Amount = amount,
                            //    ReferenceId = referenceId.ToString().ToUpper(),
                            //    TransactionType = transactionType,
                            //    TransactionDate = DateTime.UtcNow
                            //};

                            //var walletResponse = walletClient.PostAsJsonAsync(walletUrl, payload).Result;
                            //var walletContent = walletResponse.Content.ReadAsStringAsync();

                            //var walletResult = JsonConvert.DeserializeObject<BaseResponse<StatusResponse>>(walletContent.Result);

                            //if (walletResult.Result.StatusId != 1000)
                            //{
                            //    scope.Complete();
                            //    return new BaseResponse<TransferTransactionResponse>
                            //    {
                            //        StatusCode = (int)ResultType.MissingParameters,
                            //        StatusName = ResultType.MissingParameters.ToString(),
                            //        Result = new TransferTransactionResponse()
                            //    };
                            //}

                            #endregion

                            _context.TransferHistory.Add(new TransferHistory
                            {
                                ParentUserId = request.ParentAgentId,
                                UserId = request.UserId,
                                Username = balance.Username,
                                UserTypeId = balance.UserTypeId,
                                PreBalance = balance.TrueCashBalance,
                                Amount = request.TransferAmount,
                                PostBalance = (balance.TrueCashBalance + request.TransferAmount),
                                TransactionDate = dateUTCNow,
                                ReferenceId = request.ReferenceId,
                                CurrencyCode = balance.CurrencyCode,
                                IsCashPlayer = balance.IsCashPlayer,
                                TransactionTypeId = (int)transactionType,
                                TransactionTypeName = transactionType.ToString(),
                                TransactionDateOffset = dateUTCNow.AddHours(-4)
                            });

                            balance.TrueCashBalance += request.TransferAmount;
                            balance.CashBalance += request.TransferAmount;

                            var entity = _context.RunningBalance.Update(balance).Entity;

                            _context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
                            {
                                ParentUserId = entity.ParentUserId,
                                UserId = entity.UserId,
                                Username = entity.Username,
                                UserTypeId = entity.UserTypeId,
                                CurrencyCode = entity.CurrencyCode,

                                PreCashBalance = preCashBalance,
                                CashBalance = request.TransferAmount,
                                PostCashBalance = entity.CashBalance,

                                PreTrueCashBalance = preTrueCashBalance,
                                TrueCashBalance = request.TransferAmount,
                                PostTrueCashBalance = entity.TrueCashBalance,

                                PreOutstandingBalance = entity.OutstandingBalance,
                                OutstandingBalance = 0m,
                                PostOutstandingBalance = entity.OutstandingBalance,

                                IsCashPlayer = entity.IsCashPlayer,
                                ReferenceId = request.ReferenceId.ToString().ToUpper(),
                                StatusId = (int)transactionType,
                                StatusName = transactionType.ToString(),
                                TransactionDate = dateUTCNow
                            });

                            _context.SaveChanges();
                            scope.Complete();
                        }

                        return new BaseResponseDTO<PaymentTransferResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString().Replace("_", " "),
                            Result = result
                        };
                    }
                }

                // If the process has reached up to this point. Then it failed
                return new BaseResponseDTO<PaymentTransferResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace("_", " "),
                    Result = new PaymentTransferResponse()
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<PaymentTransferResponse>
                {
                    StatusCode = StatusCode.Error,
                    Message = StatusCode.Error.ToString().Replace("_", " "),
                    Result = new PaymentTransferResponse()
                };
            }
        }

        #endregion
    }
}

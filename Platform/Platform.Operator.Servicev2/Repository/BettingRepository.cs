using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Transactions;

namespace Platform.Operator.Servicev2.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private List<Tuple<string, HttpClient>> _httpClients = new List<Tuple<string, HttpClient>>();
        private readonly object _lock = new object();
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private HttpClient GetHttpClient(string threadId, string endpoint, string operatorToken, string clientId, bool isAgent, string ogPublicKey = "", string ogVendorName = "")
        {
            if (string.IsNullOrEmpty(ogPublicKey))
                return GetHttpClientSW(threadId, endpoint, operatorToken, clientId, isAgent);
            else
                return GetHttpClientOGS(threadId, endpoint, ogPublicKey, ogVendorName);
        }
        private HttpClient GetHttpClientSW(string threadId, string endpoint, string operatorToken, string clientId, bool isAgent)
        {
            var client = _httpClients.Where(c => c.Item1 == threadId).ToList();
            var httpClient = client.Where(c => c.Item2.BaseAddress.OriginalString == endpoint).FirstOrDefault();

            if (httpClient == null)
            {
                var newHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                newHttpClient.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());
                newHttpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
                newHttpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                lock (_lock)
                    _httpClients.Add(new Tuple<string, HttpClient>(threadId, newHttpClient));

                return newHttpClient;
            }
            else
            {
                httpClient.Item2.DefaultRequestHeaders.Remove("x-is-agent");
                httpClient.Item2.DefaultRequestHeaders.Remove("x-client-id");
                httpClient.Item2.DefaultRequestHeaders.Remove("x-operator-token");

                httpClient.Item2.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());
                httpClient.Item2.DefaultRequestHeaders.Add("x-client-id", clientId);
                httpClient.Item2.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                return httpClient.Item2;
            }
        }
        private HttpClient GetHttpClientOGS(string threadId, string endpoint, string key, string vendorName)
        {
            var client = _httpClients.Where(c => c.Item1 == threadId).ToList();
            var httpClient = client.Where(c => c.Item2.BaseAddress.OriginalString == endpoint).FirstOrDefault();

            if (httpClient == null)
            {
                var newHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                newHttpClient.DefaultRequestHeaders.Add("key", key);
                newHttpClient.DefaultRequestHeaders.Add("vendor-name", vendorName);

                lock (_lock)
                    _httpClients.Add(new Tuple<string, HttpClient>(threadId, newHttpClient));

                return newHttpClient;
            }
            else
            {
                httpClient.Item2.DefaultRequestHeaders.Remove("key");
                httpClient.Item2.DefaultRequestHeaders.Remove("vendor-name");

                httpClient.Item2.DefaultRequestHeaders.Add("key", key);
                httpClient.Item2.DefaultRequestHeaders.Add("vendor-name", vendorName);

                return httpClient.Item2;
            }
        }
        private void CancelBet(Guid betId, string reason, string correlationId, IMessageBroker messageBroker, bool isFreebet, bool isBoosted, string threadId)
        {
            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.Bet.Find(betId);
                        bet.Payout = bet.TrueStake;
                        bet.BetStatusId = (int)BetStatus.Cancelled;
                        bet.BetStatusName = BetStatus.Cancelled.ToString();
                        bet.Reason = $"103 - {reason}";
                        bet.SettlementDate = DateTime.UtcNow;
                        bet.SettlementDateOffset = bet.SettlementDate.AddHours(-4);
                        bet.TaxedStake = 0;
                        bet.TaxedWinnings = 0;
                        bet.IsFreeBet = isFreebet;

                        var betEntity = context.Bet.Update(bet).Entity;
                        betData = betEntity;

                        context.BetAudit.Add(new BetAudit
                        {
                            BetId = betEntity.BetId,
                            UserId = betEntity.UserId,
                            Stake = betEntity.Stake,
                            TrueStake = betEntity.TrueStake,
                            Odds = betEntity.Odds,
                            OddsTypeId = betEntity.OddsTypeId,
                            OddsTypeName = betEntity.OddsTypeName,
                            Payout = betEntity.Payout,
                            WinLoss = betEntity.WinLoss,
                            BetStatusId = betEntity.BetStatusId,
                            BetStatusName = betEntity.BetStatusName,
                            Reason = betEntity.Reason,
                            CurrencyCode = betEntity.CurrencyCode,
                            LanguageCode = betEntity.LanguageCode,
                            ExchangeRate = betEntity.ExchangeRate,
                            IPAddress = betEntity.IPAddress,
                            Channel = betEntity.Channel,
                            ClientId = betEntity.ClientId,
                            ClientIdRef = betEntity.ClientIdRef,
                            ClientCode = betEntity.ClientCode,
                            ClientName = betEntity.ClientName,
                            LimitId = betEntity.LimitId,
                            Endpoint = betEntity.Endpoint,
                            OperatorToken = betEntity.OperatorToken,
                            CompanyShare = betEntity.CompanyShare,
                            IsResettled = betEntity.IsResettled,
                            IsCashout = betEntity.IsCashout,
                            BetDate = betEntity.BetDate,
                            BetDateOffset = betEntity.BetDateOffset,
                            SettlementDate = betEntity.SettlementDate,
                            SettlementDateOffset = betEntity.SettlementDateOffset,
                            StakeTax = betEntity.StakeTax,
                            WinningsTax = betEntity.WinningsTax,
                            TaxedStake = betEntity.TaxedStake,
                            TaxedWinnings = betEntity.TaxedWinnings,
                            IsFreeBet = betEntity.IsFreeBet,
                            BetTypeId = betEntity.BetTypeId,
                            BetTypeName = betEntity.BetTypeName,
                            OddsRef = betEntity.OddsRef,
                            IsAgent = betEntity.IsAgent
                        });

                        var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                        foreach (var betSelection in betSelections)
                        {
                            betSelection.BetSelectionStatusId = bet.BetStatusId;
                            betSelection.BetSelectionStatusName = bet.BetStatusName;
                            betSelection.IsSettled = true;

                            var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                            betSelectionData.Add(betSelectionEntity);

                            context.BetSelectionAudit.Add(new BetSelectionAudit
                            {
                                BetId = betSelectionEntity.BetId,
                                BetSelectionId = betSelectionEntity.BetSelectionId,
                                UserId = betSelectionEntity.UserId,
                                SportId = betSelectionEntity.SportId,
                                SportName = betSelectionEntity.SportName,
                                SegmentId = betSelectionEntity.SegmentId,
                                SegmentName = betSelectionEntity.SegmentName,
                                LeagueId = betSelectionEntity.LeagueId,
                                LeagueName = betSelectionEntity.LeagueName,
                                FixtureId = betSelectionEntity.FixtureId,
                                FixtureName = betSelectionEntity.FixtureName,
                                FixtureTypeId = betSelectionEntity.FixtureTypeId,
                                FixtureTypeName = betSelectionEntity.FixtureTypeName,
                                MarketTemplateId = betSelectionEntity.MarketTemplateId,
                                MarketId = betSelectionEntity.MarketId,
                                MarketName = betSelectionEntity.MarketName,
                                SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                                SelectionId = betSelectionEntity.SelectionId,
                                SelectionName = betSelectionEntity.SelectionName,
                                FixtureStatusId = betSelectionEntity.FixtureStatusId,
                                FixtureStatusName = betSelectionEntity.FixtureStatusName,
                                Line = betSelectionEntity.Line,
                                Odds = betSelectionEntity.Odds,
                                BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                                BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                                IsSettled = betSelectionEntity.IsSettled,
                                IsResettled = betSelectionEntity.IsResettled,
                                FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                                SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                                FixtureStartDate = betSelectionEntity.FixtureStartDate,
                                BetScore = betSelectionEntity.BetScore,
                                SettlementScore = betSelectionEntity.SettlementScore,
                                Probability = betSelectionEntity.Probability,
                                WonProbability = betSelectionEntity.WonProbability,
                                LostProbability = betSelectionEntity.LostProbability,
                                VoidProbability = betSelectionEntity.VoidProbability,
                                HalfWonProbability = betSelectionEntity.HalfWonProbability,
                                HalfLostProbability = betSelectionEntity.HalfLostProbability,
                                CorrelationId = betSelectionEntity.CorrelationId,
                                DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                                OddsRef = betSelectionEntity.OddsRef,
                                IsBinary = betSelectionEntity.IsBinary
                            });
                        }

                        if(isBoosted == true)
                        {
                            var betPriceboost = context.BetPriceBoost.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                            betPriceboost.BetStatusId = betEntity.BetStatusId;
                            betPriceboost.BetStatusName = betEntity.BetStatusName;

                            context.BetPriceBoost.Update(betPriceboost);
                        }

                        ProcessStakeFixtureLimit(betSelectionData, context, betEntity);

                        context.SaveChanges();
                        scope.Complete();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            ProcesssBetCancellation(new OnBetSettlement
            {
                Bet = betData,
                BetSelections = betSelectionData,
                Message = reason
            }, messageBroker, correlationId, threadId);
        }
        private void AcceptBet(Guid betId, string correlationId, IMessageBroker messageBroker, OperatorBetResponseDTO operatorResponse, bool isBoosted = false, bool isBetBuilder = false, string userAgent = "")
        {
            var payload = new OnBetPlacement();
            payload.IsBoosted = isBoosted;
            payload.IsBetBuilder = isBetBuilder;
            payload.UserAgent = userAgent;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.Bet.Find(betId);

                        var betStatus = BetStatus.Running;

                        bet.BetStatusId = (int)betStatus;
                        bet.BetStatusName = betStatus.ToString();
                        bet.Reason = $"1024 - Ticket accepted (Operator)";

                        bet.ParlayBonus = operatorResponse.IsFreeBet ? 0m : bet.ParlayBonus;
                        bet.StakeReal = operatorResponse.StakeReal;
                        bet.StakeBonus = operatorResponse.StakeBonus;
                        bet.IsFreeBet = operatorResponse.IsFreeBet;

                        var betEntity = context.Bet.Update(bet).Entity;
                        payload.Bet = betEntity;

                        context.BetAudit.Add(new BetAudit
                        {
                            BetId = betEntity.BetId,
                            UserId = betEntity.UserId,
                            Stake = betEntity.Stake,
                            TrueStake = betEntity.TrueStake,
                            Odds = betEntity.Odds,
                            OddsTypeId = betEntity.OddsTypeId,
                            OddsTypeName = betEntity.OddsTypeName,
                            Payout = betEntity.Payout,
                            WinLoss = betEntity.WinLoss,
                            BetStatusId = betEntity.BetStatusId,
                            BetStatusName = betEntity.BetStatusName,
                            Reason = betEntity.Reason,
                            CurrencyCode = betEntity.CurrencyCode,
                            LanguageCode = betEntity.LanguageCode,
                            ExchangeRate = betEntity.ExchangeRate,
                            IPAddress = betEntity.IPAddress,
                            Channel = betEntity.Channel,
                            ClientId = betEntity.ClientId,
                            ClientIdRef = betEntity.ClientIdRef,
                            ClientCode = betEntity.ClientCode,
                            ClientName = betEntity.ClientName,
                            LimitId = betEntity.LimitId,
                            Endpoint = betEntity.Endpoint,
                            OperatorToken = betEntity.OperatorToken,
                            CompanyShare = betEntity.CompanyShare,
                            IsResettled = betEntity.IsResettled,
                            IsCashout = betEntity.IsCashout,
                            BetDate = betEntity.BetDate,
                            BetDateOffset = betEntity.BetDateOffset,
                            SettlementDate = betEntity.SettlementDate,
                            SettlementDateOffset = betEntity.SettlementDateOffset,
                            StakeTax = betEntity.StakeTax,
                            WinningsTax = betEntity.WinningsTax,
                            TaxedStake = betEntity.TaxedStake,
                            TaxedWinnings = betEntity.TaxedWinnings,
                            ParlayBonus = betEntity.ParlayBonus,
                            ParlayBonusWinnings = betEntity.ParlayBonusWinnings,
                            StakeReal = betEntity.StakeReal,
                            StakeBonus = betEntity.StakeBonus,
                            StakeRealPayout = betEntity.StakeRealPayout,
                            StakeBonusPayout = betEntity.StakeBonusPayout,
                            IsFreeBet = betEntity.IsFreeBet,
                            BetTypeId = betEntity.BetTypeId,
                            BetTypeName = betEntity.BetTypeName,
                            OddsRef = betEntity.OddsRef,
                            IsAgent = betEntity.IsAgent
                        });

                        var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                        foreach (var betSelection in betSelections)
                        {
                            betSelection.BetSelectionStatusId = betEntity.BetStatusId;
                            betSelection.BetSelectionStatusName = betEntity.BetStatusName;

                            var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                            payload.BetSelections.Add(betSelectionEntity);

                            context.BetSelectionAudit.Add(new BetSelectionAudit
                            {
                                BetId = betSelectionEntity.BetId,
                                BetSelectionId = betSelectionEntity.BetSelectionId,
                                UserId = betSelectionEntity.UserId,
                                SportId = betSelectionEntity.SportId,
                                SportName = betSelectionEntity.SportName,
                                SegmentId = betSelectionEntity.SegmentId,
                                SegmentName = betSelectionEntity.SegmentName,
                                LeagueId = betSelectionEntity.LeagueId,
                                LeagueName = betSelectionEntity.LeagueName,
                                FixtureId = betSelectionEntity.FixtureId,
                                FixtureName = betSelectionEntity.FixtureName,
                                FixtureTypeId = betSelectionEntity.FixtureTypeId,
                                FixtureTypeName = betSelectionEntity.FixtureTypeName,
                                MarketTemplateId = betSelectionEntity.MarketTemplateId,
                                MarketId = betSelectionEntity.MarketId,
                                MarketName = betSelectionEntity.MarketName,
                                SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                                SelectionId = betSelectionEntity.SelectionId,
                                SelectionName = betSelectionEntity.SelectionName,
                                FixtureStatusId = betSelectionEntity.FixtureStatusId,
                                FixtureStatusName = betSelectionEntity.FixtureStatusName,
                                Line = betSelectionEntity.Line,
                                Odds = betSelectionEntity.Odds,
                                OddsRef = betSelectionEntity.OddsRef,
                                BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                                BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                                IsSettled = betSelectionEntity.IsSettled,
                                IsResettled = betSelectionEntity.IsResettled,
                                FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                                SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                                FixtureStartDate = betSelectionEntity.FixtureStartDate,
                                BetScore = betSelectionEntity.BetScore,
                                SettlementScore = betSelectionEntity.SettlementScore,
                                Probability = betSelectionEntity.Probability,
                                WonProbability = betSelectionEntity.WonProbability,
                                LostProbability = betSelectionEntity.LostProbability,
                                VoidProbability = betSelectionEntity.VoidProbability,
                                HalfWonProbability = betSelectionEntity.HalfWonProbability,
                                HalfLostProbability = betSelectionEntity.HalfLostProbability,
                                CorrelationId = betSelectionEntity.CorrelationId,
                                DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                                IsBinary = betSelectionEntity.IsBinary
                            });
                        }

                        if (isBoosted == true)
                        {
                            var betPriceboost = context.BetPriceBoost.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                            betPriceboost.BetStatusId = betEntity.BetStatusId;
                            betPriceboost.BetStatusName = betEntity.BetStatusName;

                            context.BetPriceBoost.Update(betPriceboost);
                        }

                        context.SaveChanges();
                        scope.Complete();

                        if (payload.Bet.IsAgent)
                            messageBroker.Publish(payload, "OnBetPlacement", "r.bet.reports.agent", correlationId);
                        else
                            messageBroker.Publish(payload, "OnBetPlacement", "r.bet.reports", correlationId);

                        messageBroker.Publish(payload, "OnBetPlacement", "r.bet.betinfo", correlationId);

                        messageBroker.Publish(new OnBetStatus
                        {
                            ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                            UserId = payload.Bet.UserId,
                            BetId = payload.Bet.BetId.ToString().ToUpper(),
                            BetStatus = (BetStatus)payload.Bet.BetStatusId
                        }, "OnBetStatus", $"r.bet.betstatus.{payload.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        messageBroker.Publish(new OnWalletChange
                        {
                            ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                            UserId = payload.Bet.UserId,
                            Balance = operatorResponse.Balance
                        }, "OnWalletChange", $"r.wallet.{payload.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);
                    }
                    catch (Exception ex)
                    {
                        messageBroker.Publish(new OnBetPlacementError
                        {
                            BetId = betId.ToString(),
                            Error = ex.Message,
                            StackTrace = ex.StackTrace,
                            InnerException = ex.InnerException.ToString()
                        }, "OnBetPlacementError", "r.bet.placement.error", correlationId);

                    }
                }
            }
        }
        private void AddOrUpdateOperatorSettlementRetry(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, string correlationId, bool isSuccess)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using (var context = new PlatformContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.OperatorSettlementRetry.Find(message.OnBetSettlement.Bet.BetId);

                        if (isSuccess)
                        {
                            if (bet != null)
                            {
                                bet.Info = JsonSerializer.Serialize(message.OnBetSettlement);
                                bet.IsRetry = false;
                                bet.IsSuccess = isSuccess;

                                context.OperatorSettlementRetry.Update(bet);
                            }
                        }
                        else
                        {
                            if (bet == null)
                            {
                                context.OperatorSettlementRetry.Add(new OperatorSettlementRetry
                                {
                                    BetId = message.OnBetSettlement.Bet.BetId,
                                    ClientId = message.OnBetSettlement.Bet.ClientId,
                                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                                    Endpoint = message.OnBetSettlement.Bet.Endpoint,
                                    MessageType = messageType,
                                    Info = JsonSerializer.Serialize(message.OnBetSettlement),
                                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                                    CorrelationId = correlationId,
                                    RetryCount = 0,
                                    IsRetry = true,
                                    IsSuccess = false,
                                    Timestamp = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                var isRetry = bet.RetryCount < 8 ? true : false;

                                bet.Info = JsonSerializer.Serialize(message.OnBetSettlement);
                                bet.IsRetry = bet.RetryCount + 1 >= 8 ? false : true;
                                bet.RetryCount = isRetry == true ? bet.RetryCount + 1 : bet.RetryCount;

                                context.OperatorSettlementRetry.Update(bet);
                            }
                        }

                        context.SaveChanges();
                        scope.Complete();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        private static void ProcessStakeFixtureLimit(List<BetSelection> betSelectionData, BettingContext context, Bet betEntity)
        {
            if (betSelectionData.Count == 1)
            {
                var stakeLimitFixture = context.StakeLimitFixture.Find($"{betEntity.ClientCode}-{betEntity.UserId}", betSelectionData.First().FixtureId);

                if (stakeLimitFixture != null)
                {
                    stakeLimitFixture.Remaining += betEntity.TrueStake;

                    context.StakeLimitFixture.Update(stakeLimitFixture);
                }
            }
        }
        private void AddOperatorLog(OperatorLog operatorLog)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using var context = new PlatformContext(optionsBuilder.Options);
                try
                {
                    context.OperatorLog.Add(operatorLog);

                    context.SaveChanges();
                    scope.Complete();
                }
                catch (Exception)
                {
                }
            }
        }
        private Tuple<bool, OnBetSettlement> IsProcess(Guid betId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using (var context = new PlatformContext(optionsBuilder.Options))
                {
                    var bet = context.OperatorSettlementRetry.Find(betId);

                    scope.Complete();

                    if (bet == null)
                        return new Tuple<bool, OnBetSettlement>(false, JsonSerializer.Deserialize<OnBetSettlement>(bet.Info));

                    return new Tuple<bool, OnBetSettlement>(true, JsonSerializer.Deserialize<OnBetSettlement>(bet.Info));
                }
            }
        }

        private Tuple<bool, OnBetSettlementPartial> IsProcessPartial(Guid betId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using (var context = new PlatformContext(optionsBuilder.Options))
                {
                    var bet = context.OperatorSettlementRetry.Find(betId);

                    scope.Complete();

                    if (bet == null)
                        return new Tuple<bool, OnBetSettlementPartial>(false, JsonSerializer.Deserialize<OnBetSettlementPartial>(bet.Info));

                    return new Tuple<bool, OnBetSettlementPartial>(true, JsonSerializer.Deserialize<OnBetSettlementPartial>(bet.Info));
                }
            }
        }

        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            var path = "/v1/bet/placement";

            var betData = message.Bet;
            var betSelectionData = message.BetSelections;

            try
            {
                var payload = new OperatorBetRequestDTO
                {
                    TransactionId = correlationId,
                    BetId = betData.BetId.ToString().ToUpper(),
                    UserId = betData.UserId,
                    CurrencyCode = betData.CurrencyCode,
                    Stake = betData.TrueStake,
                    Odds = betData.Odds,
                    BetStatus = BetStatus.Running.ToString(),
                    StakeTax = betData.StakeTax,
                    WinningsTax = betData.WinningsTax,
                    ParlayBonus = betData.ParlayBonus,
                    TransactionDate = betData.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    IsBoosted = message.IsBoosted,
                    IsBetBuilder = message.IsBetBuilder,
                    BonusId = message.BonusId,
                    OddsRef = betData.OddsRef,
                    BetSelections = betSelectionData.Select(s => new OperatorBetSelectionRequestDTO
                    {
                        BetSelectionId = s.BetSelectionId.ToString().ToUpper(),
                        BetSelectionStatus = BetStatus.Running.ToString(),
                        SportId = s.SportId,
                        SportName = s.SportName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        FixtureId = s.FixtureId.ToString(),
                        FixtureName = s.FixtureName,
                        FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                        FixtureStatus = s.FixtureStatusName,
                        Score = s.BetScore,
                        MarketId = s.MarketId.ToString(),
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                        AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty,
                        OddsRef = s.OddsRef,
                        MarketTemplateId = s.MarketTemplateId
                    }).ToList()
                };

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{path}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, betData.Endpoint, betData.OperatorToken, betData.ClientId, betData.IsAgent).PostAsync($"{betData.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (result.IsFreeBet == true && message.IsBetBuilder)
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, result.IsFreeBet, message.IsBoosted, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        AcceptBet(message.Bet.BetId, correlationId, messageBroker, result, message.IsBoosted, message.IsBetBuilder, message.UserAgent);
                    }

                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        var betMessage = string.Empty;

                        if (result.StatusId == 1001)
                            betMessage = "Insufficient Funds";
                        else if (result.StatusId == 1002)
                            betMessage = "Invalid Token";
                        else if (result.StatusId == 1003)
                            betMessage = "Operator Error";
                        else
                            betMessage = result.Message;

                        CancelBet(betData.BetId, betMessage, Guid.NewGuid().ToString().ToUpper(), messageBroker, result.IsFreeBet, message.IsBoosted, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{path}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
            }
        }

        public void ProcesssBetPlacementOG(OnBetPlacement message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            var path = "/api/v2/platform/transaction/bulk/debit";

            var betData = message.Bet;
            var betSelectionData = message.BetSelections;

            try
            {

                var publicKey = _configuration["Custom:OrientalGaming:PublicAPIKey"].ToString();
                var privateKey = _configuration["Custom:OrientalGaming:PrivateAPIKey"].ToString();
                var vendorName = _configuration["Custom:OrientalGaming:VendorName"].ToString();

                var records = new List<object> { new {
                       amount = betData.TrueStake,
                       bet_place = "",
                       transaction_id = correlationId,
                       secondary_info = new { bet_amount = betData.TrueStake, detain_amount = 0m },
                       other_info = betSelectionData,
                       remark = ""
                    } };

                var payload = new OnOGDebitRequest
                {
                    PlayerCode = betData.UserId,
                    TimeStamp = 0, //get from request
                    TotalAmount = betData.TrueStake,
                    TransactionType = "debit",
                    GameCode = "ogsports", //get from request
                    RoundId = betData.BetId.ToString().ToUpper(),
                    Currency = betData.CurrencyCode,
                    CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Records = JsonSerializer.Serialize(records),
                    Signature = ""
                };

                //signature generation
                var signature = payload.ToSignOG(privateKey);
                payload.Signature = signature;

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{path}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, betData.Endpoint, betData.OperatorToken, betData.ClientId, betData.IsAgent, publicKey, vendorName).PostAsync($"{betData.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OGDebitResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if(result.Code != "S-100")
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        var operatorResponse = new OperatorBetResponseDTO
                        {
                            Balance = result.Balance,
                            IsFreeBet = false,
                            IsCashoutDisabled = false,
                            Message = JsonSerializer.Serialize(result.Message),
                            StatusId = (int)StatusCode.Success,
                            StakeReal = betData.TrueStake,
                            StakeBonus = 0m,
                        };

                        AcceptBet(message.Bet.BetId, correlationId, messageBroker, operatorResponse, message.IsBoosted, message.IsBetBuilder, message.UserAgent);
                    }
                    
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        var betMessage = string.Empty;

                        if (result.StatusId == 1001)
                            betMessage = "Insufficient Funds";
                        else if (result.StatusId == 1002)
                            betMessage = "Invalid Token";
                        else if (result.StatusId == 1003)
                            betMessage = "Operator Error";
                        else
                            betMessage = result.Message;

                        CancelBet(betData.BetId, betMessage, Guid.NewGuid().ToString().ToUpper(), messageBroker, result.IsFreeBet, message.IsBoosted, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{path}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{path}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
            }
        }

        public void ProcessBetFullCashOut(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/v1/bet/full/cashout";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
                NonTaxedPayout = message.NonTaxedPayout,
                Stake = message.Bet.Stake,
                Odds = message.Bet.Odds,
                BetStatus = message.Bet.BetStatusName,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                TaxedStake = message.Bet.TaxedStake,
                TaxedWinnings = message.Bet.TaxedWinnings,
                TransactionDate = message.Bet.SettlementDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = message.BetSelections.Select(s => new OperatorBetSelectionSettlementRequestDTO
                {
                    BetSelectionId = s.BetSelectionId.ToString().ToUpper(),
                    BetSelectionStatus = s.BetSelectionStatusName,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName,
                    FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    FixtureStatus = s.FixtureStatusName,
                    Score = s.BetScore,
                    SettlementScore = s.SettlementScore,
                    MarketId = s.MarketId.ToString(),
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    MarketTemplateId = s.MarketTemplateId,
                    HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
                }).ToList()
            };

            try
            {
                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetFullCashOut", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetFullCashOut", "r.bet.reports", correlationId);
                }

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    messageBroker.Publish(new OnWalletChange
                    {
                        ClientId = message.Bet.ClientId.ToUpper(),
                        UserId = message.Bet.UserId,
                        Balance = result.Balance
                    }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, payload, "OnBetFullCashOut", correlationId, response.IsSuccessStatusCode);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetFullCashOut", messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetFullCashOut", messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetFullCashOut", messageBroker, correlationId, threadId);
            }
        }

        public void ProcessBetPartialCashOut(OnBetSettlementPartial message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcessPartial(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/v1/bet/partial/cashout";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.CashoutStake,
                NonTaxedPayout = message.NonTaxedPayout,
                Stake = message.Stake,
                Odds = message.Bet.Odds,
                BetStatus = message.Bet.BetStatusName,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                TaxedStake = message.TaxedStake,
                TaxedWinnings = message.Bet.TaxedWinnings,
                TransactionDate = message.Bet.SettlementDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = message.BetSelections.Select(s => new OperatorBetSelectionSettlementRequestDTO
                {
                    BetSelectionId = s.BetSelectionId.ToString().ToUpper(),
                    BetSelectionStatus = s.BetSelectionStatusName,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName,
                    FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    FixtureStatus = s.FixtureStatusName,
                    Score = s.BetScore,
                    SettlementScore = s.SettlementScore,
                    MarketId = s.MarketId.ToString(),
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    MarketTemplateId = s.MarketTemplateId,
                    HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
                }).ToList()
            };

            try
            {
                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetPartialCashOut", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetPartialCashOut", "r.bet.reports", correlationId);
                }

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    messageBroker.Publish(new OnWalletChange
                    {
                        ClientId = message.Bet.ClientId.ToUpper(),
                        UserId = message.Bet.UserId,
                        Balance = result.Balance
                    }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, payload, "OnBetPartialCashOut", correlationId, response.IsSuccessStatusCode);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetPartialCashOut", messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetFullCashOut", messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, path, "OnBetFullCashOut", messageBroker, correlationId, threadId);
            }
        }

        public void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/v1/bet/settlement";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
                NonTaxedPayout = message.NonTaxedPayout,
                Stake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                BetStatus = message.Bet.BetStatusName,
                StakeTax = message.Bet.StakeTax,
                WinningsTax = message.Bet.WinningsTax,
                TaxedStake = message.Bet.TaxedStake,
                TaxedWinnings = message.Bet.TaxedWinnings,
                ParlayBonusWinnings = message.Bet.ParlayBonusWinnings,
                ParlayBonus = message.Bet.ParlayBonus,
                StakeReal = message.Bet.StakeReal,
                StakeBonus = message.Bet.StakeBonus,
                StakeRealPayout = message.Bet.StakeRealPayout,
                StakeBonusPayout = message.Bet.StakeBonusPayout,
                TransactionDate = message.Bet.SettlementDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = message.BetSelections.Select(s => new OperatorBetSelectionSettlementRequestDTO
                {
                    BetSelectionId = s.BetSelectionId.ToString().ToUpper(),
                    BetSelectionStatus = s.BetSelectionStatusName,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName,
                    FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    FixtureStatus = s.FixtureStatusName,
                    Score = s.BetScore,
                    SettlementScore = s.SettlementScore,
                    MarketId = s.MarketId.ToString(),
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty,
                    DeadHeatFactor = s.DeadHeatFactor,
                    MarketTemplateId = s.MarketTemplateId,
                }).ToList()
            };

            try
            {
                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetSettlement", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetSettlement", "r.bet.reports", correlationId);
                }

                if (message.IsAudit)
                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "REQUEST",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(payload),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (message.IsAudit)
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        messageBroker.Publish(new OnBetStatus
                        {
                            ClientId = message.Bet.ClientId.ToUpper(),
                            UserId = message.Bet.UserId,
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            BetStatus = (BetStatus)message.Bet.BetStatusId,
                            Message = message.Message
                        }, "OnBetStatus", $"r.bet.betstatus.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        messageBroker.Publish(new OnWalletChange
                        {
                            ClientId = message.Bet.ClientId.ToUpper(),
                            UserId = message.Bet.UserId,
                            Balance = result.Balance
                        }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, payload, "OnBetSettlement", correlationId, response.IsSuccessStatusCode);
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        if (message.IsAudit)
                        {
                            AddOperatorLog(new OperatorLog
                            {
                                Direction = "RESPONSE",
                                ClientId = message.Bet.ClientId,
                                ClientCode = message.Bet.ClientCode,
                                URL = $"{message.Bet.Endpoint}{path}",
                                BetId = message.Bet.BetId.ToString().ToUpper(),
                                Info = JsonSerializer.Serialize(result),
                                OperatorToken = message.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        if (message.IsAudit)
                        {
                            AddOperatorLog(new OperatorLog
                            {
                                Direction = "RESPONSE",
                                ClientId = message.Bet.ClientId,
                                ClientCode = message.Bet.ClientCode,
                                URL = $"{message.Bet.Endpoint}{path}",
                                BetId = message.Bet.BetId.ToString().ToUpper(),
                                Info = JsonSerializer.Serialize(response.ReasonPhrase),
                                OperatorToken = message.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                if (message.IsAudit)
                {
                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId, threadId);
            }
        }
        public void ProcesssBetCancellation(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/v1/bet/cancellation";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
                Stake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                BetStatus = message.Bet.BetStatusName,
                TransactionDate = message.Bet.SettlementDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                IsBetBuilder = message.Bet.BetTypeId == (int)BetType.BetBuilder ? true : false,
                BetSelections = message.BetSelections.Select(s => new OperatorBetSelectionSettlementRequestDTO
                {
                    BetSelectionId = s.BetSelectionId.ToString().ToUpper(),
                    BetSelectionStatus = s.BetSelectionStatusName,
                    SportId = s.SportId,
                    SportName = s.SportName,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName,
                    FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    FixtureStatus = s.FixtureStatusName,
                    Score = s.BetScore,
                    SettlementScore = s.SettlementScore,
                    MarketId = s.MarketId.ToString(),
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty,
                    MarketTemplateId = s.MarketTemplateId
                }).ToList()
            };

            try
            {
                
                messageBroker.Publish(new OnBetStatus
                {
                    ClientId = message.Bet.ClientId.ToString().ToUpper(),
                    UserId = message.Bet.UserId,
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    BetStatus = (BetStatus)message.Bet.BetStatusId,
                    Message = message.Message
                }, "OnBetStatus", $"r.bet.betstatus.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetCancellation", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetCancellation", "r.bet.reports", correlationId);
                }


                messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackOfficeCancellation", "r.bet.cancellation", correlationId);

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    messageBroker.Publish(new OnWalletChange
                    {
                        ClientId = message.Bet.ClientId.ToUpper(),
                        UserId = message.Bet.UserId,
                        Balance = result.Balance
                    }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, payload, "OnBetCancellation", correlationId, response.IsSuccessStatusCode);
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                        RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                        RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId, threadId);
            }
        }

        private void RetryBetCashOut(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string path, string messageType, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            try
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId, message.OnBetSettlement.Bet.IsAgent).PostAsync($"{message.OnBetSettlement.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                        BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(message, payload, path, messageType, messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCashOut(message, payload, path, messageType, messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetCashOut(message, payload, path, messageType, messageBroker, correlationId, threadId);
            }
        }
        private void RetryBetSettlement(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            var path = "/v1/bet/settlement";

            try
            {
                if (message.OnBetSettlement.IsAudit)
                {
                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "REQUEST",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                        BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(payload),
                        OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var response = GetHttpClient(threadId, message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId, message.OnBetSettlement.Bet.IsAgent).PostAsync($"{message.OnBetSettlement.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (message.OnBetSettlement.IsAudit)
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        if (message.OnBetSettlement.IsAudit)
                        {
                            AddOperatorLog(new OperatorLog
                            {
                                Direction = "RESPONSE",
                                ClientId = message.OnBetSettlement.Bet.ClientId,
                                ClientCode = message.OnBetSettlement.Bet.ClientCode,
                                URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                                BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                                Info = JsonSerializer.Serialize(result),
                                OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        RetryBetSettlement(message, payload, messageType, messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        if (message.OnBetSettlement.IsAudit)
                        {
                            AddOperatorLog(new OperatorLog
                            {
                                Direction = "RESPONSE",
                                ClientId = message.OnBetSettlement.Bet.ClientId,
                                ClientCode = message.OnBetSettlement.Bet.ClientCode,
                                URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                                BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                                Info = JsonSerializer.Serialize(response.ReasonPhrase),
                                OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        RetryBetSettlement(message, payload, messageType, messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                if (message.OnBetSettlement.IsAudit)
                {
                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                        BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                        OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                RetryBetSettlement(message, payload, messageType, messageBroker, correlationId, threadId);
            }
        }
        private void RetryBetCancellation(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            var path = "/v1/bet/cancellation";

            try
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId, message.OnBetSettlement.Bet.IsAgent).PostAsync($"{message.OnBetSettlement.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                        BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCancellation(message, payload, messageType, messageBroker, correlationId, threadId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCancellation(message, payload, messageType, messageBroker, correlationId, threadId);
                    }
                }
            }
            catch (Exception ex)
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{path}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(new { message = ex.Message, stackTrace = ex.StackTrace }),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetCancellation(message, payload, messageType, messageBroker, correlationId, threadId);
            }
        }
    }
}

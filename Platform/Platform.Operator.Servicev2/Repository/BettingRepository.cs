using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Platform.Operator.Servicev2.Telemetry;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Transactions;

namespace Platform.Operator.Servicev2.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private List<Tuple<string, HttpClient>> _httpClients = new List<Tuple<string, HttpClient>>();
        private readonly object _lock = new object();
        private readonly OrientalGamingConfigDTO _ogConfig;
        private readonly string _bettingConnectionString;
        private readonly string _platformConnectionString;
        private readonly ILogger<BettingRepository> _logger;

        private readonly ActivitySource _activitySource;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public BettingRepository(IConfiguration configuration, Instrumentation instrumentation, ILogger<BettingRepository> logger)
        {
            _bettingConnectionString = configuration.GetConnectionString("Betting");
            _platformConnectionString = configuration.GetConnectionString("Platform");
            _ogConfig = configuration.GetSection("Custom").GetSection("OrientalGaming").Get<OrientalGamingConfigDTO>();
            _activitySource = instrumentation.ActivitySource;
            _logger = logger;
        }

        private HttpClient GetHttpClient(string threadId, string endpoint, string operatorToken, string clientId, bool isAgent, string ogPublicKey = "", string ogVendorName = "")
        {
            if (string.IsNullOrEmpty(ogPublicKey))
                return GetHttpClientSW(threadId, endpoint, operatorToken.GetActualValue(), clientId, isAgent);
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
        private void CancelBet(Guid betId, string reason, string correlationId, IMessageBroker messageBroker, bool isFreebet, bool isBoosted, string threadId, bool isOGClient = false, OGRequestDTO OGParameters = null)
        {
            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_bettingConnectionString);

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

                        if (isBoosted == true)
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
                        _logger.LogError($"error during cancel bet: {ex.Message}. stack trace: {ex.StackTrace}");
                    }
                }
            }

            if (isOGClient == false)
            {
                ProcesssBetCancellation(new OnBetSettlement
                {
                    Bet = betData,
                    BetSelections = betSelectionData,
                    Message = reason
                }, messageBroker, correlationId, threadId);
            }
            else
            {
                ProcesssBetCancellationOG(new OnBetSettlement
                {
                    Bet = betData,
                    BetSelections = betSelectionData,
                    Message = reason
                }, messageBroker, correlationId, threadId, OGParameters);
            }
        }
        private void AcceptBet(Guid betId, string correlationId, IMessageBroker messageBroker, OperatorBetResponseDTO operatorResponse, bool isBoosted = false, bool isBetBuilder = false, string userAgent = "")
        {
            var payload = new OnBetPlacement();
            payload.IsBoosted = isBoosted;
            payload.IsBetBuilder = isBetBuilder;
            payload.UserAgent = userAgent;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_bettingConnectionString);

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
                            //payload.BetSelections.Add(betSelectionEntity); 
                            payload.BetSelections.Add(new BetSelectionMessageExtenstion()
                            {
                                BetSelection = betSelectionEntity,
                                //MarketSelectionCoumt = message.BetSelections.FirstOrDefault(c => c.BetSelection.BetId == betSelection.BetId && c.BetSelection.BetSelectionId == betSelection.BetSelectionId).MarketSelectionCoumt
                            });

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
        private void AddOrUpdateOperatorSettlementRetry(OnBetSettlementRetry message, string messageType, string correlationId, bool isSuccess)
        {
            using var activity = this._activitySource.StartActivity("AddOrUpdateOperatorSettlementRetry");

            activity?.SetTag("bet.id", message.OnBetSettlement.Bet.BetId);
            activity?.SetTag("message.issuccess", isSuccess);

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_platformConnectionString);

                using (var context = new PlatformContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.OperatorSettlementRetry.Find(message.OnBetSettlement.Bet.BetId);

                        if (isSuccess)
                        {
                            if (bet != null)
                            {
                                activity?.SetTag("bet.isnull", false);

                                bet.MessageType = messageType;
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
                                activity?.SetTag("bet.isnull", true);

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

                                activity?.SetTag("bet.isretry", isRetry);

                                bet.MessageType = messageType;
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
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_platformConnectionString);

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
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_platformConnectionString);

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
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_platformConnectionString);

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
                var currencyFromOperatorToken = message.Bet.OperatorToken.GetCurrencyCode();

                var payload = new OperatorBetRequestDTO
                {
                    TransactionId = correlationId,
                    BetId = betData.BetId.ToString().ToUpper(),
                    UserId = betData.UserId,
                    CurrencyCode = string.IsNullOrEmpty(currencyFromOperatorToken) ? betData.CurrencyCode : currencyFromOperatorToken,
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
                    BetSelections = betSelectionData.Select(s => s.BetSelection).Select(s => new OperatorBetSelectionRequestDTO
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

                        foreach (var selection in message.BetSelections.Where(c => c.MarketSelectionCoumt <= 3))
                        {
                            messageBroker.Publish(new OnClientTopMarketBetPlacement
                            {
                                ClientId = betData.ClientId.ToUpper(),
                                ClientCode = betData.ClientCode.ToUpper(),
                                ClientName = betData.ClientName,
                                CurrencyCode = betData.CurrencyCode,
                                SportId = selection.BetSelection.SportId,
                                SportName = selection.BetSelection.SportName,
                                FixtureId = selection.BetSelection.FixtureId,
                                FixtureName = selection.BetSelection.FixtureName,
                                FixtureTypeId = selection.BetSelection.FixtureTypeId,
                                FixtureTypeName = selection.BetSelection.FixtureTypeName,
                                MarketId = selection.BetSelection.MarketId,
                                MarketName = selection.BetSelection.MarketName,
                                MarketStatusId = (int)MarketStatus.Active,
                                MarketStatusName = MarketStatus.Active.ToString(),
                                MarketSelectionCount = selection.MarketSelectionCoumt
                            }, "OnBetPlacement", $"r.client.topmarket.placement", correlationId);
                        }
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

            try
            {
                using var activity = this._activitySource.StartActivity("ProcesssBetPlacementOG");

                activity?.SetTag("bet.id", message.Bet.BetId);
                
                var publicKey = _ogConfig.PublicAPIKey;
                var privateKey = _ogConfig.PrivateAPIKey;
                var vendorName = _ogConfig.VendorName;
                var ogParam = message.Bet.OperatorToken.ToOGParam();

                var betSelectionData = new
                {
                    BetId = message.Bet.BetId,
                    UserId = message.Bet.UserId,
                    CurrencyCode = ogParam.OGCurrencyCode,
                    bet_status = BetStatus.Running.ToString(),
                    ParlayBonus = message.Bet.ParlayBonus.Parse(),
                    ParlayBonusWinnings = GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.ParlayBonusWinnings).Parse(),
                    odds_type = message.Bet.OddsTypeName,
                    total_odds = message.Bet.Odds.Parse(),
                    parlay_type = GetParlayCountLabel(message.BetSelections.Count),
                    bet_list = message.BetSelections.Select(s => s.BetSelection).Select(s => new
                    {
                        sport_code = s.SportId.ToString(),
                        sport_name = new
                        {
                            en = s.SportName
                        },
                        play_name = new
                        {
                            en = s.MarketName.ConvertToUnicodeEscape()
                        },
                        score = s.BetScore,
                        odds = s.Odds.Parse(),
                        bet_item = new
                        {
                            en = s.SelectionName.ConvertToUnicodeEscape()
                        },
                        away_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        home_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        league_name = new
                        {
                            en = s.LeagueName
                        },
                        game_date_time = s.FixtureStartDate.GetEpochTimestamp(),
                        BetSelectionId = s.BetSelectionId,
                        bet_selection_status = BetStatus.Running.ToString(),
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        FixtureId = s.FixtureId.ToString(),
                        FixtureName = s.FixtureName.ConvertToUnicodeEscape(),
                        FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                        FixtureStatus = s.FixtureStatusName,
                        FixtureType = s.FixtureTypeName,
                        settlement_score = s.SettlementScore,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketId = s.MarketId.ToString(),
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        DeadHeatFactor = s.DeadHeatFactor.Parse()
                    }).ToList()
                };

                var records = new[] { new {
                           amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, betData.TrueStake).Truncate()),
                           bet_place = message.BetSelections.Count > 1 ? "multiple" : "single",
                           transaction_id = betData.BetId.ToString().ToLower().Replace("-",""),
                           secondary_info = new { bet_amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, betData.TrueStake).Truncate()), detain_amount = 0 },
                           other_info = betSelectionData,
                           remark = (string)null
                        }};

                var payload = new OnOGDebitRequest
                {
                    PlayerCode = betData.UserId,
                    TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TotalAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, betData.TrueStake).Truncate()),
                    TransactionType = "debit",
                    GameCode = ogParam.GameCode,
                    RoundId = "na",
                    Currency = ogParam.OGCurrencyCode,
                    CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Records = records,
                    Signature = ""
                };

                //signature generation
                var signature = payload.ToSignOG(privateKey);
                payload.Signature = signature;

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter() },
                };

                var info = JsonSerializer.Serialize(payload, options);

                var operatorPayload = new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{path}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = info,
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                AddOperatorLog(operatorPayload);

                var response = GetHttpClient(threadId, betData.Endpoint, betData.OperatorToken, betData.ClientId, betData.IsAgent, publicKey, vendorName).PostAsync($"{betData.Endpoint}{path}", new StringContent(info, System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OGDebitResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (result.Code != "S-100")
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
                            Balance = Convert.ToDecimal(result.Balance),
                            IsFreeBet = false,
                            IsCashoutDisabled = false,
                            Message = JsonSerializer.Serialize(result.Message),
                            StatusId = (int)StatusCode.Success,
                            StakeReal = betData.TrueStake,
                            StakeBonus = 0m,
                        };

                        AcceptBet(message.Bet.BetId, correlationId, messageBroker, operatorResponse, message.IsBoosted, message.IsBetBuilder, message.UserAgent);

                        foreach (var selection in message.BetSelections.Where(c => c.MarketSelectionCoumt <= 3))
                        {
                            messageBroker.Publish(new OnClientTopMarketBetPlacement
                            {
                                ClientId = betData.ClientId.ToUpper(),
                                ClientCode = betData.ClientCode.ToUpper(),
                                ClientName = betData.ClientName,
                                CurrencyCode = betData.CurrencyCode,
                                SportId = selection.BetSelection.SportId,
                                SportName = selection.BetSelection.SportName,
                                FixtureId = selection.BetSelection.FixtureId,
                                FixtureName = selection.BetSelection.FixtureName,
                                FixtureTypeId = selection.BetSelection.FixtureTypeId,
                                FixtureTypeName = selection.BetSelection.FixtureTypeName,
                                MarketId = selection.BetSelection.MarketId,
                                MarketName = selection.BetSelection.MarketName,
                                MarketStatusId = (int)MarketStatus.Active,
                                MarketStatusName = MarketStatus.Active.ToString(),
                                MarketSelectionCount = selection.MarketSelectionCoumt
                            }, "OnBetPlacement", $"r.client.topmarket.placement", correlationId);
                        }
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OGDebitResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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

                        if (result.Code == "S-103")
                            betMessage = "Insufficient Funds";
                        else if (result.Code == "E-106")
                            betMessage = "Invalid Token";
                        else if (result.Code == "E-000")
                            betMessage = "Operator Error";
                        else
                            betMessage = result.Message;

                        CancelBet(betData.BetId, betMessage, Guid.NewGuid().ToString().ToUpper(), messageBroker, false, message.IsBoosted, threadId);
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
            var currencyFromOperatorToken = message.Bet.OperatorToken.GetCurrencyCode();

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = string.IsNullOrEmpty(currencyFromOperatorToken) ? message.Bet.CurrencyCode : currencyFromOperatorToken,
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

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetFullCashOut", correlationId, response.IsSuccessStatusCode);
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

        public void ProcessBetFullCashOutOG(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId, OGRequestDTO ogParam)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/api/v2/platform/transaction/credit";

            try
            {
                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetFullCashOut", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetFullCashOut", "r.bet.reports", correlationId);
                }

                var publicKey = _ogConfig.PublicAPIKey;
                var privateKey = _ogConfig.PrivateAPIKey;
                var vendorName = _ogConfig.VendorName;
                var transType = "credit";

                var betSelectionData = new
                {
                    BetId = message.Bet.BetId,
                    UserId = message.Bet.UserId,
                    CurrencyCode = ogParam.OGCurrencyCode,
                    bet_status = message.Bet.BetStatusName,
                    ParlayBonus = message.Bet.ParlayBonus.Parse(),
                    ParlayBonusWinnings = GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.ParlayBonusWinnings).Parse(),
                    odds_type = message.Bet.OddsTypeName,
                    total_odds = message.Bet.Odds.Parse(),
                    parlay_type = GetParlayCountLabel(message.BetSelections.Count),
                    bet_list = message.BetSelections.Select(s => new
                    {
                        sport_code = s.SportId.ToString(),
                        sport_name = new
                        {
                            en = s.SportName
                        },
                        play_name = new
                        {
                            en = s.MarketName.ConvertToUnicodeEscape()
                        },
                        score = s.BetScore,
                        odds = s.Odds.Parse(),
                        bet_item = new
                        {
                            en = s.SelectionName.ConvertToUnicodeEscape()
                        },
                        away_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        home_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        league_name = new
                        {
                            en = s.LeagueName
                        },
                        game_date_time = s.FixtureStartDate.GetEpochTimestamp(),
                        BetSelectionId = s.BetSelectionId,
                        bet_selection_status = s.BetSelectionStatusName,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        FixtureId = s.FixtureId.ToString(),
                        FixtureName = s.FixtureName.ConvertToUnicodeEscape(),
                        FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                        FixtureStatus = s.FixtureStatusName,
                        FixtureType = s.FixtureTypeName,
                        settlement_score = s.SettlementScore,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketId = s.MarketId.ToString(),
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        DeadHeatFactor = s.DeadHeatFactor.Parse()
                    }).ToList()
                };


                var payload = new OnOGCreditRequest
                {
                    PlayerCode = message.Bet.UserId,
                    TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.Payout).Truncate()),
                    TransactionId = message.Bet.BetId.ToString().ToLower().Replace("-", ""),
                    TransactionType = transType,
                    GameCode = ogParam.GameCode,
                    RoundId = "na",
                    BetPlace = message.BetSelections.Count > 1 ? "multiple" : "single",
                    Currency = ogParam.OGCurrencyCode,
                    WinloseAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.WinLoss).Truncate()),
                    EffectiveAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()),
                    ResultUrl = $"result?client_id={message.Bet.ClientId}&transaction_id={message.Bet.BetId}",
                    SecondaryInfo = new { bet_amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()), detain_amount = 0 },
                    OtherInfo = betSelectionData,
                    CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Signature = ""
                };

                //signature generation
                var signature = payload.ToSignOG(privateKey);
                payload.Signature = signature;

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter() },
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload, options),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload, options), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OGCreditResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (result.Code == "S-100")
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

                        messageBroker.Publish(new OnWalletChange
                        {
                            ClientId = message.Bet.ClientId.ToUpper(),
                            UserId = message.Bet.UserId,
                            Balance = Convert.ToDecimal(result.Balance)
                        }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetFullCashOut", correlationId, response.IsSuccessStatusCode);
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
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetFullCashOut", correlationId, false);
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
                    }

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetFullCashOut", correlationId, false);
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

                AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetFullCashOut", correlationId, false);
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
            var currencyFromOperatorToken = message.Bet.OperatorToken.GetCurrencyCode();

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = string.IsNullOrEmpty(currencyFromOperatorToken) ? message.Bet.CurrencyCode : currencyFromOperatorToken,
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

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetPartialCashOut", correlationId, response.IsSuccessStatusCode);
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
            var currencyFromOperatorToken = message.Bet.OperatorToken.GetCurrencyCode();

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = string.IsNullOrEmpty(currencyFromOperatorToken) ? message.Bet.CurrencyCode : currencyFromOperatorToken,
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

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetSettlement", correlationId, response.IsSuccessStatusCode);
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

        public void ProcesssBetSettlementOG(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId, OGRequestDTO ogParam, bool isOGResettle)
        {
            using var activity = this._activitySource.StartActivity("ProcesssBetSettlementOG");

            activity?.SetTag("bet.id", message.Bet.BetId);
            activity?.SetTag("message.isretry", message.IsRetry);
            activity?.SetTag("message.isresettle", message.IsResettle);
            activity?.SetTag("message.isaudit", message.IsAudit);

            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var isCancelSuccess = false;
            var path = "/api/v2/platform/transaction/credit";

            try
            {

                if (message.IsRetry == false)
                {
                    if (message.Bet.IsAgent)
                        messageBroker.Publish(message, "OnBetSettlement", "r.bet.reports.agent", correlationId);
                    else
                        messageBroker.Publish(message, "OnBetSettlement", "r.bet.reports", correlationId);
                }

                var publicKey = _ogConfig.PublicAPIKey;
                var privateKey = _ogConfig.PrivateAPIKey;
                var vendorName = _ogConfig.VendorName;
                var transType = "credit";

                var betSelectionData = new
                {
                    BetId = message.Bet.BetId,
                    UserId = message.Bet.UserId,
                    CurrencyCode = ogParam.OGCurrencyCode,
                    bet_status = message.Bet.BetStatusName.ToOGStatus(),
                    ParlayBonus = message.Bet.ParlayBonus.Parse(),
                    ParlayBonusWinnings = GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.ParlayBonusWinnings).Parse(),
                    odds_type = message.Bet.OddsTypeName,
                    total_odds = message.Bet.Odds.Parse(),
                    parlay_type = GetParlayCountLabel(message.BetSelections.Count),
                    bet_list = message.BetSelections.Select(s => new
                    {
                        sport_code = s.SportId.ToString(),
                        sport_name = new
                        {
                            en = s.SportName
                        },
                        play_name = new
                        {
                            en = s.MarketName.ConvertToUnicodeEscape()
                        },
                        score = s.BetScore,
                        odds = s.Odds.Parse(),
                        bet_item = new
                        {
                            en = s.SelectionName.ConvertToUnicodeEscape()
                        },
                        away_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        home_team = new
                        {
                            en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim().ConvertToUnicodeEscape() : (string)null
                        },
                        league_name = new
                        {
                            en = s.LeagueName
                        },
                        game_date_time = s.FixtureStartDate.GetEpochTimestamp(),
                        BetSelectionId = s.BetSelectionId,
                        bet_selection_status = s.BetSelectionStatusName.ToOGStatus(),
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        LeagueId = s.LeagueId,
                        FixtureId = s.FixtureId.ToString(),
                        FixtureName = s.FixtureName.ConvertToUnicodeEscape(),
                        FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                        FixtureStatus = s.FixtureStatusName,
                        FixtureType = s.FixtureTypeName,
                        settlement_score = s.SettlementScore,
                        MarketTemplateId = s.MarketTemplateId,
                        MarketId = s.MarketId.ToString(),
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        DeadHeatFactor = s.DeadHeatFactor.Parse()
                    }).ToList()
                };

                var payload = new OnOGCreditRequest
                {
                    PlayerCode = message.Bet.UserId,
                    TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.Payout).Truncate()),
                    TransactionId = message.Bet.BetId.ToString().ToLower().Replace("-", ""),
                    TransactionType = transType,
                    GameCode = ogParam.GameCode,
                    RoundId = "na",
                    BetPlace = message.BetSelections.Count > 1 ? "multiple" : "single",
                    Currency = ogParam.OGCurrencyCode,
                    WinloseAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.WinLoss).Truncate()),
                    EffectiveAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()),
                    ResultUrl = $"/result?client_id={message.Bet.ClientId}&transaction_id={message.Bet.BetId}",
                    SecondaryInfo = new { bet_amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()), detain_amount = 0 },
                    OtherInfo = betSelectionData,
                    CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Signature = ""
                };

                //signature generation
                var signature = payload.ToSignOG(privateKey);
                payload.Signature = signature;

                if (message.IsResettle && isOGResettle == false)
                {
                    transType = "cancel";

                    var cancelPayload = new OnOGCreditCancelRequest
                    {
                        PlayerCode = message.Bet.UserId,
                        TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        TransactionId = message.Bet.BetId.ToString().ToLower().Replace("-", ""),
                        TransactionType = transType,
                        SecondaryInfo = new { bet_amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()), detain_amount = 0 },
                        OtherInfo = new { betSelectionData },
                        CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Signature = ""
                    };

                    //signature generation
                    var cancelSignature = cancelPayload.ToSignOG(privateKey);
                    cancelPayload.Signature = cancelSignature;

                    if (message.IsAudit)
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "REQUEST",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{path}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(cancelPayload),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                    var cancelOptions = new JsonSerializerOptions
                    {
                        Converters = { new DecimalJsonConverter() }
                    };

                    var cancelResponse = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(cancelPayload, cancelOptions), System.Text.Encoding.UTF8, "application/json")).Result;
                    if (cancelResponse.IsSuccessStatusCode)
                    {
                        if (message.IsAudit)
                        {
                            var result = JsonSerializer.Deserialize<OGCreditResponseDTO>(cancelResponse.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            if (result.Code == "S-100")
                            {
                                isCancelSuccess = true;
                                transType = "resettle";
                                payload.TransactionType = transType;

                                //signature generation
                                signature = payload.ToSignOG(privateKey);
                                payload.Signature = signature;
                            }

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
                                Info = cancelResponse.Content.ReadAsStringAsync().Result,
                                OperatorToken = message.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }

                    if (transType == "cancel")
                    {
                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetSettlement", correlationId, false);
                        return;
                    }
                }

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter() },
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                if (message.IsAudit)
                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "REQUEST",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{path}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(payload, options),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload, options), System.Text.Encoding.UTF8, "application/json")).Result;

                activity?.SetTag("response.status", response.IsSuccessStatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responsePayload = response.Content.ReadAsStringAsync().Result;

                    var result = JsonSerializer.Deserialize<OGCreditResponseDTO>(responsePayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    activity?.SetTag("response.payload", responsePayload);

                    if (result.Code == "S-100")
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
                                Balance = Convert.ToDecimal(result.Balance)
                            }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        }

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetSettlement", correlationId, response.IsSuccessStatusCode);
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
                                Info = JsonSerializer.Serialize(result),
                                OperatorToken = message.Bet.OperatorToken,
                                CorrelationId = correlationId,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetSettlementOG", correlationId, false);
                    }

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                else
                {
                    activity?.SetStatus(ActivityStatusCode.Error, response.StatusCode.ToString());

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (message.IsAudit)
                        {
                            var result = JsonSerializer.Deserialize<OGCreditResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            activity?.SetTag("response.bad.request.payload", JsonSerializer.Serialize(result));

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
                        else
                            activity?.SetTag("response.error.payload", JsonSerializer.Serialize(response.ReasonPhrase));

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
                        activity?.SetTag("response.error.payload", JsonSerializer.Serialize(response.ReasonPhrase));
                    }
                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetSettlementOG", correlationId, false);
                }

            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

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
                AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, isCancelSuccess ? "OnBetSettlementOG" : "OnBetSettlement", correlationId, false);           
            }
            finally
            {
                activity?.Stop();
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
            var currencyFromOperatorToken = message.Bet.OperatorToken.GetCurrencyCode();

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = string.IsNullOrEmpty(currencyFromOperatorToken) ? message.Bet.CurrencyCode : currencyFromOperatorToken,
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

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetCancellation", correlationId, response.IsSuccessStatusCode);
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

        public void ProcesssBetCancellationOG(OnBetSettlement message, IMessageBroker messageBroker, string correlationId, string threadId, OGRequestDTO ogParam)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var path = "/api/v2/platform/transaction/bulk/debit";

            var publicKey = _ogConfig.PublicAPIKey;
            var privateKey = _ogConfig.PrivateAPIKey;
            var vendorName = _ogConfig.VendorName;

            var betSelectionData = new
            {
                BetId = message.Bet.BetId,
                UserId = message.Bet.UserId,
                CurrencyCode = ogParam.OGCurrencyCode,
                bet_status = message.Bet.BetStatusName.ToOGStatus(),
                ParlayBonus = message.Bet.ParlayBonus.Parse(),
                ParlayBonusWinnings = GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.ParlayBonusWinnings).Parse(),
                odds_type = message.Bet.OddsTypeName,
                total_odds = message.Bet.Odds.Parse(),
                parlay_type = GetParlayCountLabel(message.BetSelections.Count),
                bet_list = message.BetSelections.Select(s => new
                {
                    sport_code = s.SportId.ToString(),
                    sport_name = new
                    {
                        en = s.SportName
                    },
                    play_name = new
                    {
                        en = s.MarketName.ConvertToUnicodeEscape()
                    },
                    score = s.BetScore,
                    odds = s.Odds.Parse(),
                    bet_item = new
                    {
                        en = s.SelectionName.ConvertToUnicodeEscape()
                    },
                    away_team = new
                    {
                        en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim().ConvertToUnicodeEscape() : (string)null
                    },
                    home_team = new
                    {
                        en = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim().ConvertToUnicodeEscape() : (string)null
                    },
                    league_name = new
                    {
                        en = s.LeagueName
                    },
                    game_date_time = s.FixtureStartDate.GetEpochTimestamp(),
                    BetSelectionId = s.BetSelectionId,
                    bet_selection_status = s.BetSelectionStatusName.ToOGStatus(),
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    LeagueId = s.LeagueId,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName.ConvertToUnicodeEscape(),
                    FixtureStartDate = s.FixtureStartDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                    FixtureStatus = s.FixtureStatusName,
                    FixtureType = s.FixtureTypeName,
                    settlement_score = s.SettlementScore,
                    MarketTemplateId = s.MarketTemplateId,
                    MarketId = s.MarketId.ToString(),
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    DeadHeatFactor = s.DeadHeatFactor.Parse()
                }).ToList()
            };

            var records = new[] { new {
                           amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()),
                           bet_place = message.BetSelections.Count > 1 ? "multiple" : "single",
                           transaction_id = message.Bet.BetId.ToString().ToLower().Replace("-",""),
                           secondary_info = new { bet_amount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()), detain_amount = 0 },
                           other_info = betSelectionData,
                           remark = (string)null
                        }};

            var payload = new OnOGDebitRequest
            {
                PlayerCode = message.Bet.UserId,
                TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TotalAmount = Convert.ToDouble(GetOgRedenominatedValue(ogParam.OGCurrencyCode, message.Bet.TrueStake).Truncate()),
                TransactionType = "rollback",
                GameCode = ogParam.GameCode,
                RoundId = "na",
                Currency = ogParam.OGCurrencyCode,
                CalledAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Records = records,
                Signature = ""
            };

            //signature generation
            var signature = payload.ToSignOG(privateKey);
            payload.Signature = signature;

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

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter() }
                };

                var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload, options), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OGDebitResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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

                    if (result.Code == "S-100")
                    {
                        messageBroker.Publish(new OnWalletChange
                        {
                            ClientId = message.Bet.ClientId.ToUpper(),
                            UserId = message.Bet.UserId,
                            Balance = Convert.ToDecimal(result.Balance)
                        }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);

                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetCancellation", correlationId, response.IsSuccessStatusCode);
                    }
                    else
                    {
                        var isRetry = true;
                        if (result.Code == "S-101")
                        {
                            var transactionStatus = GetOGTransactionStatus(message, "rollback", correlationId, threadId, publicKey, vendorName, privateKey);
                            if (transactionStatus.TransactionStatus == "rollback")
                            {
                                // wallet endpoint
                                var walletRequest = new
                                {
                                    player_code = payload.PlayerCode,
                                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                };
                                var signatureEx = walletRequest.ToSignOG(privateKey);
                                var walletResponse = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).GetAsync($"{message.Bet.Endpoint}/api/v2/platform/player/balance?player_code={walletRequest.player_code}&timestamp={walletRequest.timestamp}&signature={signatureEx}").Result;
                                var walletResult = JsonSerializer.Deserialize<OGWalletResponseDTO>(walletResponse.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = false });
                                if (walletResult.Code == "S-100")
                                {
                                    messageBroker.Publish(new OnWalletChange
                                    {
                                        ClientId = message.Bet.ClientId.ToUpper(),
                                        UserId = message.Bet.UserId,
                                        Balance = Convert.ToDecimal(walletResult.Balance)
                                    }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);
                                }
                                AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetCancellation", correlationId, true);
                                isRetry = false;
                            }
                        }

                        if (isRetry)
                        {
                            messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                            AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetCancellation", correlationId, false);
                        }
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OGDebitResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetCancellation", correlationId, false);
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
                        AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetCancellation", correlationId, false);
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

                var isRetry = true;
                var transactionStatus = GetOGTransactionStatus(message, "rollback", correlationId, threadId, publicKey, vendorName, privateKey);
                if (transactionStatus.TransactionStatus == "rollback")
                {
                    // wallet endpoint
                    var walletRequest = new
                    {
                        player_code = payload.PlayerCode,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                    var signatureEx = walletRequest.ToSignOG(privateKey);
                    var walletResponse = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).GetAsync($"{message.Bet.Endpoint}/api/v2/platform/player/balance?player_code={walletRequest.player_code}&timestamp={walletRequest.timestamp}&signature={signatureEx}").Result;
                    var walletResult = JsonSerializer.Deserialize<OGWalletResponseDTO>(walletResponse.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = false });
                    if (walletResult.Code == "S-100")
                    {
                        messageBroker.Publish(new OnWalletChange
                        {
                            ClientId = message.Bet.ClientId.ToUpper(),
                            UserId = message.Bet.UserId,
                            Balance = Convert.ToDecimal(walletResult.Balance)
                        }, "OnWalletChange", $"r.wallet.{message.Bet.ClientCode.ToString().ToLower()}", correlationId, 10);
                    }
                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, "OnBetCancellation", correlationId, true);
                    isRetry = false;
                }

                if (isRetry)
                {
                    messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, "OnBetCancellation", correlationId, false);
                }
            }
        }

        private void RetryBetCashOut(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string path, string messageType, IMessageBroker messageBroker, string correlationId, string threadId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, messageType, correlationId, false);
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
                AddOrUpdateOperatorSettlementRetry(message, messageType, correlationId, false);
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
                AddOrUpdateOperatorSettlementRetry(message, messageType, correlationId, false);
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

        private OGTransactionResponseDTO GetOGTransactionStatus(OnBetSettlement message, string expectedStatus, string correlationId, string threadId, string publicKey, string vendorName, string privateKey)
        {
            var path = "/api/v2/platform/vendor/transaction-status";

            var payload = new
            {
                player_code = message.Bet.UserId,
                transaction_id = message.Bet.BetId.ToString().ToLower().Replace("-", "")
            };


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

            var response = GetHttpClient(threadId, message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId, message.Bet.IsAgent, publicKey, vendorName).PostAsync($"{message.Bet.Endpoint}{path}", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<OGTransactionResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                if (result.Code == "S-100" && result.TransactionStatus == expectedStatus)
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
                    return result;
                }

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
            else
                AddOperatorLog(new OperatorLog
                {
                    Direction = "RESPONSE",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{path}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(response),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

            return new OGTransactionResponseDTO();
        }

        private decimal GetOgRedenominatedValue(string ogCurrencyCode, decimal value)
        {
            if (_ogConfig.HighCurrencies.Contains(ogCurrencyCode))
                return value / 1000;

            return value;
        }

        private string GetParlayCountLabel(int selectionCount)
        {
            return selectionCount switch
            {
                1 => (string)null,
                2 => "Double (1 Bet)",
                3 => "Treble (1 Bet)",
                _ => $"{selectionCount}-Folds"
            };
        }

    }
}

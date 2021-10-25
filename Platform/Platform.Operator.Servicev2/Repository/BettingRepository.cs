using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
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
        private List<HttpClient> _httpClients = new List<HttpClient>();
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private HttpClient GetHttpClient(string endpoint, string operatorToken, string clientId)
        {
            var result = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(5)
            };

            result.DefaultRequestHeaders.Add("x-client-id", clientId);
            result.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            return result;
        }

        //private HttpClient GetHttpClient(string endpoint, string operatorToken)
        //{
        //    var result = _httpClients.Where(c => c.BaseAddress.OriginalString == endpoint).FirstOrDefault();

        //    if (result == null)
        //    {
        //        var newHttpClient = new HttpClient
        //        {
        //            BaseAddress = new Uri(endpoint),
        //            Timeout = TimeSpan.FromSeconds(5)
        //        };

        //        newHttpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

        //        _httpClients.Add(newHttpClient);

        //        return newHttpClient;
        //    }
        //    else
        //    {
        //        result.DefaultRequestHeaders.Remove("x-operator-token");
        //        result.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

        //        return result;
        //    }

        //}
        private void CancelBet(Guid betId, string reason, string correlationId, IMessageBroker messageBroker)
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
                            TaxedWinnings = betEntity.TaxedWinnings
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
                                DeadHeatFactor = betSelectionEntity.DeadHeatFactor
                            });
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
            }, messageBroker, correlationId);
        }
        private void AcceptBet(Guid betId, string correlationId, IMessageBroker messageBroker, OperatorBetResponseDTO operatorResponse)
        {
            var payload = new OnBetPlacement();

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
                            IsFreeBet = betEntity.IsFreeBet
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
                                DeadHeatFactor = betSelectionEntity.DeadHeatFactor
                            });
                        }

                        context.SaveChanges();
                        scope.Complete();

                        messageBroker.Publish(payload, "OnBetPlacement", "r.bet.reports", correlationId);

                        messageBroker.Publish(new OnBetStatus
                        {
                            ClientId = payload.Bet.ClientId.ToString().ToUpper(),
                            UserId = payload.Bet.UserId,
                            BetId = payload.Bet.BetId.ToString().ToUpper(),
                            BetStatus = (BetStatus)payload.Bet.BetStatusId
                        }, "OnBetStatus", "r.bet.betstatus", correlationId);
                    }
                    catch (Exception)
                    {
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
                                bet.IsRetry = bet.RetryCount + 1 == 8 ? false : true;
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

        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {
            var uri = "/v1/bet/placement";

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
                    Stake = betData.Stake,
                    Odds = betData.Odds,
                    BetStatus = BetStatus.Running.ToString(),
                    StakeTax = betData.StakeTax,
                    WinningsTax = betData.WinningsTax,
                    TransactionDate = betData.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
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
                        AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
                    }).ToList()
                };

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = betData.ClientId,
                    ClientCode = betData.ClientCode,
                    URL = $"{betData.Endpoint}{uri}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(betData.Endpoint, betData.OperatorToken, betData.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = betData.ClientId,
                        ClientCode = betData.ClientCode,
                        URL = $"{betData.Endpoint}{uri}",
                        BetId = betData.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = betData.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    AcceptBet(message.Bet.BetId, correlationId, messageBroker, result);
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
                            URL = $"{betData.Endpoint}{uri}",
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

                        CancelBet(betData.BetId, betMessage, Guid.NewGuid().ToString().ToUpper(), messageBroker);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = betData.ClientId,
                            ClientCode = betData.ClientCode,
                            URL = $"{betData.Endpoint}{uri}",
                            BetId = betData.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = betData.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker);
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
                    URL = $"{betData.Endpoint}{uri}",
                    BetId = betData.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = betData.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                CancelBet(betData.BetId, "Operator Error", Guid.NewGuid().ToString().ToUpper(), messageBroker);
            }
        }
        public void ProcessBetFullCashOut(OnBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var uri = "/v1/bet/full/cashout";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
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
                    HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").First().Trim() : string.Empty,
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
                }).ToList()
            };

            try
            {
                if (message.IsRetry == false)
                    messageBroker.Publish(message, "OnBetFullCashOut", "r.bet.reports", correlationId);

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{uri}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

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
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetFullCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetFullCashOut", messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetFullCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetFullCashOut", messageBroker, correlationId);
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
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetFullCashOut(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetFullCashOut", messageBroker, correlationId);
            }
        }
        public void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var uri = "/v1/bet/settlement";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
                Stake = message.Bet.Stake,
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
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
                }).ToList()
            };

            try
            {
                if (message.IsRetry == false)
                    messageBroker.Publish(message, "OnBetSettlement", "r.bet.reports", correlationId);

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{uri}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    AddOrUpdateOperatorSettlementRetry(new OnBetSettlementRetry { OnBetSettlement = message }, payload, "OnBetSettlement", correlationId, response.IsSuccessStatusCode);
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
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId);
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
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetSettlement(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetSettlement", messageBroker, correlationId);
            }
        }
        public void ProcesssBetCancellation(OnBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            if (message.IsRetry)
            {
                var initialMessage = IsProcess(message.Bet.BetId);

                if (initialMessage.Item1 == false)
                    return;

                message = initialMessage.Item2;
                message.IsRetry = true;
            }

            var uri = "/v1/bet/cancellation";

            var payload = new OperatorBetSettlementRequestDTO
            {
                TransactionId = correlationId,
                BetId = message.Bet.BetId.ToString().ToUpper(),
                UserId = message.Bet.UserId,
                CurrencyCode = message.Bet.CurrencyCode,
                Payout = message.Bet.Payout,
                Stake = message.Bet.Stake,
                Odds = message.Bet.Odds,
                BetStatus = message.Bet.BetStatusName,
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
                    AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureName.Split(" vs. ").Last().Trim() : string.Empty
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
                }, "OnBetStatus", "r.bet.betstatus", correlationId);

                if (message.IsRetry == false)
                    messageBroker.Publish(message, "OnBetCancellation", "r.bet.reports", correlationId);

                messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackOfficeCancellation", "r.bet.cancellation", correlationId);

                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.Bet.ClientId,
                    ClientCode = message.Bet.ClientCode,
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.Bet.Endpoint, message.Bet.OperatorToken, message.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.Bet.ClientId,
                        ClientCode = message.Bet.ClientCode,
                        URL = $"{message.Bet.Endpoint}{uri}",
                        BetId = message.Bet.BetId.ToString().ToUpper(),
                        Info = JsonSerializer.Serialize(result),
                        OperatorToken = message.Bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

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
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                        RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.Bet.ClientId,
                            ClientCode = message.Bet.ClientCode,
                            URL = $"{message.Bet.Endpoint}{uri}",
                            BetId = message.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                        RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId);
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
                    URL = $"{message.Bet.Endpoint}{uri}",
                    BetId = message.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                messageBroker.Publish(new OnBetBackofficeCancellation { BetId = message.Bet.BetId }, "OnBetBackofficeCancellation", "r.bet.cancellation", correlationId);
                RetryBetCancellation(new OnBetSettlementRetry { RemainingRetries = 0, OnBetSettlement = message }, payload, "OnBetCancellation", messageBroker, correlationId);
            }
        }

        private void RetryBetFullCashOut(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, IMessageBroker messageBroker, string correlationId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            var uri = "/v1/bet/full/cashout";

            try
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
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
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetFullCashOut(message, payload, messageType, messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetFullCashOut(message, payload, messageType, messageBroker, correlationId);
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
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetFullCashOut(message, payload, messageType, messageBroker, correlationId);
            }
        }
        private void RetryBetSettlement(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, IMessageBroker messageBroker, string correlationId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            var uri = "/v1/bet/settlement";

            try
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
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
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetSettlement(message, payload, messageType, messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetSettlement(message, payload, messageType, messageBroker, correlationId);
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
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetSettlement(message, payload, messageType, messageBroker, correlationId);
            }
        }
        private void RetryBetCancellation(OnBetSettlementRetry message, OperatorBetSettlementRequestDTO payload, string messageType, IMessageBroker messageBroker, string correlationId)
        {
            if (message.RemainingRetries == 0)
            {
                AddOrUpdateOperatorSettlementRetry(message, payload, messageType, correlationId, false);
                return;
            }

            message.RemainingRetries -= 1;

            var uri = "/v1/bet/cancellation";

            try
            {
                AddOperatorLog(new OperatorLog
                {
                    Direction = "REQUEST",
                    ClientId = message.OnBetSettlement.Bet.ClientId,
                    ClientCode = message.OnBetSettlement.Bet.ClientCode,
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(payload),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                var response = GetHttpClient(message.OnBetSettlement.Bet.Endpoint, message.OnBetSettlement.Bet.OperatorToken, message.OnBetSettlement.Bet.ClientId).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    AddOperatorLog(new OperatorLog
                    {
                        Direction = "RESPONSE",
                        ClientId = message.OnBetSettlement.Bet.ClientId,
                        ClientCode = message.OnBetSettlement.Bet.ClientCode,
                        URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
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
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(result),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCancellation(message, payload, messageType, messageBroker, correlationId);
                    }
                    else
                    {
                        AddOperatorLog(new OperatorLog
                        {
                            Direction = "RESPONSE",
                            ClientId = message.OnBetSettlement.Bet.ClientId,
                            ClientCode = message.OnBetSettlement.Bet.ClientCode,
                            URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                            BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                            Info = JsonSerializer.Serialize(response.ReasonPhrase),
                            OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.UtcNow
                        });

                        RetryBetCancellation(message, payload, messageType, messageBroker, correlationId);
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
                    URL = $"{message.OnBetSettlement.Bet.Endpoint}{uri}",
                    BetId = message.OnBetSettlement.Bet.BetId.ToString().ToUpper(),
                    Info = JsonSerializer.Serialize(ex.Message),
                    OperatorToken = message.OnBetSettlement.Bet.OperatorToken,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                });

                RetryBetCancellation(message, payload, messageType, messageBroker, correlationId);
            }
        }
    }
}

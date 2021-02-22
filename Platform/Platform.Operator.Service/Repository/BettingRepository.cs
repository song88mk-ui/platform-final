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

namespace Platform.Operator.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private List<HttpClient> _httpClients = new List<HttpClient>();
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        private HttpClient GetHttpClient(string endpoint, string operatorToken)
        {
            var result = _httpClients.Where(c => c.BaseAddress.OriginalString == endpoint).FirstOrDefault();

            if (result == null)
            {
                var newHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                newHttpClient.DefaultRequestHeaders.Remove("x-operator-token");
                newHttpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                _httpClients.Add(newHttpClient);

                return newHttpClient;
            }
            else
                return result;
        }
        private Tuple<Bet, List<BetSelection>> GetBet(Guid betId)
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
                        betData = bet;

                        var betSelections = context.BetSelection.AsNoTracking().Where(c => c.BetId == bet.BetId).ToList();
                        betSelectionData.AddRange(betSelections);

                        scope.Complete();
                    }
                    catch (Exception)
                    {
                        return new Tuple<Bet, List<BetSelection>>(null, null);
                    }
                }
            }

            return new Tuple<Bet, List<BetSelection>>(betData, betSelectionData);
        }
        private void CancelBet(Guid betId, string reason, string correlationId, IMessageBroker messageBroker)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                using (var context = new BettingContext(optionsBuilder.Options))
                {
                    try
                    {
                        var bet = context.Bet.Find(betId);
                        bet.BetStatusId = (int)BetStatus.Cancelled;
                        bet.BetStatusName = BetStatus.Cancelled.ToString();
                        bet.Reason = $"103 - {reason}";

                        var betEntity = context.Bet.Update(bet).Entity;

                        context.BetAudit.Add(new BetAudit
                        {
                            BetId = bet.BetId,
                            UserId = bet.UserId,
                            Stake = bet.Stake,
                            TrueStake = bet.TrueStake,
                            Odds = bet.Odds,
                            OddsTypeId = bet.OddsTypeId,
                            OddsTypeName = bet.OddsTypeName,
                            Payout = bet.Payout,
                            WinLoss = bet.WinLoss,
                            BetStatusId = bet.BetStatusId,
                            BetStatusName = bet.BetStatusName,
                            Reason = bet.Reason,
                            CurrencyCode = bet.CurrencyCode,
                            LanguageCode = bet.LanguageCode,
                            ExchangeRate = bet.ExchangeRate,
                            IPAddress = bet.IPAddress,
                            Channel = bet.Channel,
                            ClientId = bet.ClientId,
                            ClientCode = bet.ClientCode,
                            LimitId = bet.LimitId,
                            Endpoint = bet.Endpoint,
                            OperatorToken = bet.OperatorToken,
                            CompanyShare = bet.CompanyShare,
                            IsResettled = bet.IsResettled,
                            IsCashout = bet.IsCashout,
                            BetDate = bet.BetDate,
                            BetDateOffset = bet.BetDateOffset,
                            SettlementDate = bet.SettlementDate,
                            SettlementDateOffset = bet.SettlementDateOffset
                        });

                        var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                        foreach (var betSelection in betSelections)
                        {
                            betSelection.BetSelectionStatusId = bet.BetStatusId;
                            betSelection.BetSelectionStatusName = bet.BetStatusName;

                            context.BetSelection.Update(betSelection);

                            context.BetSelectionAudit.Add(new BetSelectionAudit
                            {
                                BetId = betSelection.BetId,
                                BetSelectionId = betSelection.BetSelectionId,
                                UserId = betSelection.UserId,
                                SportId = betSelection.SportId,
                                SportName = betSelection.SportName,
                                SegmentId = betSelection.SegmentId,
                                SegmentName = betSelection.SegmentName,
                                LeagueId = betSelection.LeagueId,
                                LeagueName = betSelection.LeagueName,
                                FixtureId = betSelection.FixtureId,
                                FixtureName = betSelection.FixtureName,
                                FixtureTypeId = betSelection.FixtureTypeId,
                                FixtureTypeName = betSelection.FixtureTypeName,
                                MarketTemplateId = betSelection.MarketTemplateId,
                                MarketId = betSelection.MarketId,
                                MarketName = betSelection.MarketName,
                                SelectionTemplateId = betSelection.SelectionTemplateId,
                                SelectionId = betSelection.SelectionId,
                                SelectionName = betSelection.SelectionName,
                                FixtureStatusId = betSelection.FixtureStatusId,
                                FixtureStatusName = betSelection.FixtureStatusName,
                                Line = betSelection.Line,
                                Odds = betSelection.Odds,
                                BetSelectionStatusId = betSelection.BetSelectionStatusId,
                                BetSelectionStatusName = betSelection.BetSelectionStatusName,
                                IsSettled = false,
                                IsResettled = false,
                                FixtureSpecifier = betSelection.FixtureSpecifier,
                                SelectionSpecifier = betSelection.SelectionSpecifier,
                                FixtureStartDate = betSelection.FixtureStartDate,
                                BetHomeScore = betSelection.BetHomeScore,
                                BetAwayScore = betSelection.BetAwayScore,
                                SettlementHomeScore = string.Empty,
                                SettlementAwayScore = string.Empty,
                                CorrelationId = betSelection.CorrelationId
                            });
                        }

                        context.SaveChanges();
                        scope.Complete();

                        ProcesssBetCancellation(new OnBetSettlement
                        {
                            Bet = betEntity,
                            BetSelections = betSelections
                        }, messageBroker, correlationId);
                    }
                    catch (Exception)
                    {
                    }
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
        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {
            var uri = "/v1/bet/placement";

            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            var bet = GetBet(message.BetId);
            betData = bet.Item1;
            betSelectionData = bet.Item2;

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
                    BetStatus = betData.BetStatusName,
                    TransactionDate = betData.BetDate,
                    BetSelections = betSelectionData.Select(s => new OperatorBetSelectionRequestDTO
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
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatus = s.FixtureStatusName,
                        Score = $"{s.BetHomeScore} - {s.BetAwayScore}",
                        MarketId = s.MarketId.ToString(),
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").First().Trim() : string.Empty,
                        AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").Last().Trim() : string.Empty
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

                var response = GetHttpClient(betData.Endpoint, betData.OperatorToken).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

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

                        CancelBet(betData.BetId, JsonSerializer.Serialize(result), Guid.NewGuid().ToString().ToUpper(), messageBroker);
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

                        CancelBet(betData.BetId, JsonSerializer.Serialize(response.ReasonPhrase), Guid.NewGuid().ToString().ToUpper(), messageBroker);
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

                CancelBet(betData.BetId, JsonSerializer.Serialize(ex.Message), Guid.NewGuid().ToString().ToUpper(), messageBroker);
            }
        }
        public void ProcesssBetSettlement(OnBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            var uri = "/v1/bet/settlement";

            try
            {
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
                    TransactionDate = message.Bet.SettlementDate,
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
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatus = s.FixtureStatusName,
                        Score = $"{s.BetHomeScore} - {s.BetAwayScore}",
                        SettlementScore = $"{s.SettlementHomeScore} - {s.SettlementAwayScore}",
                        MarketId = s.MarketId.ToString(),
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").First().Trim() : string.Empty,
                        AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").Last().Trim() : string.Empty
                    }).ToList()
                };

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

                var response = GetHttpClient(message.Bet.Endpoint, message.Bet.OperatorToken).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

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

                        //TODO: implement retry
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

                        //TODO: implement retry
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

                //TODO: implement retry
            }
        }
        public void ProcesssBetCancellation(OnBetSettlement message, IMessageBroker messageBroker, string correlationId)
        {
            var uri = "/v1/bet/cancellation";

            try
            {
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
                    TransactionDate = message.Bet.SettlementDate,
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
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatus = s.FixtureStatusName,
                        Score = $"{s.BetHomeScore} - {s.BetAwayScore}",
                        SettlementScore = $"{s.SettlementHomeScore} - {s.SettlementAwayScore}",
                        MarketId = s.MarketId.ToString(),
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        HomeName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").First().Trim() : string.Empty,
                        AwayName = s.FixtureTypeId == (int)FixtureType.Match ? s.FixtureTypeName.Split(" - ").Last().Trim() : string.Empty
                    }).ToList()
                };

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

                var response = GetHttpClient(message.Bet.Endpoint, message.Bet.OperatorToken).PostAsync(uri, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

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

                        //TODO: implement retry
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

                        //TODO: implement retry
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

                //TODO: implement retry
            }
        }
    }
}

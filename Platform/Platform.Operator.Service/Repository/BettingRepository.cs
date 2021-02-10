using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.API.Data;
using Platform.Operator.Service.Data;
using Platform.Operator.Service.Data.Entities;
using Platform.Operator.Service.Entities;
using Platform.Operator.Service.Utility;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
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
        private readonly HttpClient _httpClient;
        public BettingRepository(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
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

                        var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();
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

        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
        {
            var uri = "/v1/bet/placement";

            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            var bet = GetBet(message.BetId);
            betData = bet.Item1;
            betSelectionData = bet.Item2;

            _httpClient.BaseAddress = new Uri(betData.Endpoint);
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", betData.OperatorToken);

            try
            {
                var payload = new OperatorBetRequestDTO
                {
                    TransactionId = Guid.NewGuid().ToString().ToUpper(),
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

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                    using var context = new PlatformContext(optionsBuilder.Options);
                    try
                    {
                        context.OperatorLog.Add(new OperatorLog
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

                        context.SaveChanges();
                        scope.Complete();
                    }
                    catch (Exception)
                    {
                    }
                }

                var response = _httpClient.PostAsync($"/v1/bet/placement", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                    var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                    using var context = new PlatformContext(optionsBuilder.Options);
                    try
                    {
                        context.OperatorLog.Add(new OperatorLog
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

                        context.SaveChanges();
                        scope.Complete();
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                        var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                        using var context = new PlatformContext(optionsBuilder.Options);
                        try
                        {
                            context.OperatorLog.Add(new OperatorLog
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

                            context.SaveChanges();
                            scope.Complete();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                        var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                        using var context = new PlatformContext(optionsBuilder.Options);
                        try
                        {
                            context.OperatorLog.Add(new OperatorLog
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

                            context.SaveChanges();
                            scope.Complete();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using var context = new PlatformContext(optionsBuilder.Options);
                try
                {
                    context.OperatorLog.Add(new OperatorLog
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

                    context.SaveChanges();
                    scope.Complete();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}

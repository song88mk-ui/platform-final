using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Operator.Service.Data;
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
        public void ProcesssBetPlacement(OnBetPlacement message, IMessageBroker messageBroker, string correlationId)
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
                        var bet = context.Bet.Find(message.BetId);
                        betData = bet;

                        var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();
                        betSelectionData.AddRange(betSelections);

                        scope.Complete();
                    }
                    catch (Exception)
                    {
                        scope.Dispose();
                        return;
                    }
                }
            }

            _httpClient.BaseAddress = new Uri(betData.Endpoint);
            _httpClient.DefaultRequestHeaders.Add("x-operator-token", betData.OperatorToken);

            try
            {
                var payload = new OperatorBetRequestDTO2
                {
                    TransactionId = Guid.NewGuid().ToString().ToUpper(),
                    BetId = betData.BetId.ToString().ToUpper(),
                    UserId = betData.UserId,
                    CurrencyCode = betData.CurrencyCode,
                    Stake = betData.Stake,
                    Odds = betData.Odds,
                    BetStatus = betData.BetStatusName,
                    TransactionDate = betData.BetDate,
                    BetSelections = betSelectionData.Select(s => new OperatorBetSelectionRequestDTO2
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

                var response = _httpClient.PostAsync($"/v1/bet/placement", new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OperatorBetResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {

                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class OperatorBetRequestDTO2
    {
        public OperatorBetRequestDTO2()
        {
            BetSelections = new List<OperatorBetSelectionRequestDTO2>();
        }
        public string TransactionId { get; set; }
        public string BetId { get; set; }
        public string UserId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public string BetStatus { get; set; }
        public DateTime TransactionDate { get; set; }
        public List<OperatorBetSelectionRequestDTO2> BetSelections { get; set; }
    }

    public class OperatorBetSelectionRequestDTO2
    {
        public string BetSelectionId { get; set; }
        public string BetSelectionStatus { get; set; }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string FixtureName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public string FixtureStatus { get; set; }
        public string Score { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public decimal Odds { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
    }
}

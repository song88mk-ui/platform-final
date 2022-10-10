using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

namespace Platform.Operator.Cashout.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly ILogger<BettingRepository> _logger;
        private readonly IConfiguration _configuration;
        private List<HttpClient> _httpClients = new List<HttpClient>();
        public BettingRepository(ILogger<BettingRepository> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private HttpClient GetHttpClient(string endpoint, string clientId)
        {
            var result = new HttpClient
            {
                BaseAddress = new Uri(endpoint),
                Timeout = TimeSpan.FromSeconds(5)
            };

            result.DefaultRequestHeaders.Add("x-client-id", clientId);

            return result;
        }

        public void ProcessCashOutResult(OnCashOutResult message, IMessageBroker messageBroker, string correlationId)
        {
            var endpoint = message.Endpoint;
            var path = "/v1/cashout/status";

            try
            {
                if(string.IsNullOrEmpty(endpoint)) //temporary fix for null endpoint
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                        using (var context = new BettingContext(optionsBuilder.Options))
                        {
                            var bet = context.Bet.Find(Guid.Parse(message.BetId));
                            if (bet == null)
                            {
                                _logger.LogInformation($"Error - Bet Id not found: {message.BetId} - {JsonSerializer.Serialize(message)}");
                                return;
                            }

                            endpoint = bet.Endpoint;
                        }
                    }

                }

                var cashout = new CashoutResultRequestDTO
                {
                    BetId = message.BetId,
                    BetStatus = message.BetStatus.ToString(),
                    CashOutStake = message.CashOutStake,
                    CashOutStatus = message.CashOutStatus.ToString(),
                    ClientCode = message.ClientCode,
                    Message = message.Message,
                    UserId = message.UserId
                };

                _logger.LogInformation($"ProcessCashOut - Request: {JsonSerializer.Serialize(cashout)}");

                var response = GetHttpClient(endpoint, message.ClientId).PostAsync($"{endpoint}{path}", new StringContent(JsonSerializer.Serialize(cashout), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<CashoutResultResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    _logger.LogInformation($"ProcessCashOut - Response: {message.BetId} - {response.Content.ReadAsStringAsync().Result}");
                }
                else
                {
                    _logger.LogInformation($"ProcessCashOut - Response: {message.BetId} - {JsonSerializer.Serialize(response)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message} -> CashOutResult: {message.BetId} - {JsonSerializer.Serialize(message)}");
            }
        }
    }
}

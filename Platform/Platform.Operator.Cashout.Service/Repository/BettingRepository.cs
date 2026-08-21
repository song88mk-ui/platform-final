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
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly ILogger<BettingRepository> _logger;
        private List<HttpClient> _httpClients = new List<HttpClient>();
        public BettingRepository(ILogger<BettingRepository> logger)
        {
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
                    var result = JsonSerializer.Deserialize<CashoutResultResponseDTO>(response.Content.ReadAsStringAsync().Result, _jsonOptions);
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

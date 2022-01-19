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

namespace Platform.Operator.Cashout.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IConfiguration _configuration;
        private List<HttpClient> _httpClients = new List<HttpClient>();
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
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

                var response = GetHttpClient(endpoint, message.ClientId).PostAsync($"{endpoint}{path}", new StringContent(JsonSerializer.Serialize(cashout), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<CashoutResultResponseDTO>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    //TODO : Log To file
                    
                }
                else
                {
                    //TODO : Log To file
                }
            }
            catch (Exception ex)
            {
                //TODO : Log To file
                Console.WriteLine($"{DateTime.UtcNow} : Error: {ex.Message} -> CashOutResult: {JsonSerializer.Serialize(message)}");
            }
        }
    }
}

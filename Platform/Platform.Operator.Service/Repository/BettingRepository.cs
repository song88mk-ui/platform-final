using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Operator.Service.Data;
using Platform.Operator.Service.Entities;
using Platform.Operator.Service.Utility;
using Platform.Shared.MessageModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            //_httpClient.DefaultRequestHeaders.Add("x-operator-token", betData.);

            //try
            //{
            //    var response = await _httpClient.PostAsync($"/v1/authentication", new StringContent(JsonSerializer.Serialize(new { }), System.Text.Encoding.UTF8, "application/json"));

            //    if (response.IsSuccessStatusCode)
            //    {
            //        var result = JsonSerializer.Deserialize<AuthenticationResponseDTO>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            //        return new BaseResponseDTO<AuthenticationResponseDTO>
            //        {
            //            StatusCode = StatusCode.Success,
            //            Message = StatusCode.Success.ToString(),
            //            Result = new AuthenticationResponseDTO
            //            {
            //                UserId = result.UserId,
            //                Balance = result.Balance,
            //                CurrencyCode = result.CurrencyCode,
            //                StatusId = result.StatusId
            //            }
            //        };
            //    }
            //    else
            //    {
            //        if (response.StatusCode == HttpStatusCode.BadRequest)
            //        {
            //            return new BaseResponseDTO<AuthenticationResponseDTO>
            //            {
            //                StatusCode = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers,
            //                Message = StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers.ToString().Replace('_', ' '),
            //            };
            //        }
            //        else
            //        {
            //            return new BaseResponseDTO<AuthenticationResponseDTO>
            //            {
            //                StatusCode = StatusCode.Operator_Error,
            //                Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
            //            };
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return new BaseResponseDTO<AuthenticationResponseDTO>
            //    {
            //        StatusCode = StatusCode.Operator_Error,
            //        Message = $"{StatusCode.Operator_Error.ToString().Replace('_', ' ')} ({ex.Message})",
            //    };
            //}
        }
    }
}

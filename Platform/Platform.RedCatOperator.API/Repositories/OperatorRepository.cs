using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.RedCatOperator.API.Repositories
{
    public class OperatorRepository : IOperatorRepository
    {
        public readonly PlatformContext _platformContext;
        private readonly IConfiguration _configuration;

        private readonly object _lock = new object();
        private List<HttpClient> _httpClients = new List<HttpClient>();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();
        public OperatorRepository(PlatformContext platformContext)
        {
            _platformContext = platformContext;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequest request)
        {
            var path = $"/vs/bet/placement";
            var endpointUrl = $"{request.Endpoint}{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new
            {

                BetId = request.BetIdRef,
                MarktId = request.BetMarketId,
                GameId = request.GameId,
                BetStatusId = request.BetStatusId,
                BetStatusName = request.BetStatusName,
                Stake = request.Stake,
                Reason = string.Empty
            };

            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(endpointUrl).PostAsync(endpointUrl, httpContent);
                    var responsePayload = response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : response.ReasonPhrase;

                    AddOperatorLog(request, "RESPONSE", endpointUrl, responsePayload, request.CorrelationId);

                    if (!response.IsSuccessStatusCode)
                    {
                        //await RetryBetPlacement(request, payload, endpointUrl, request.CorrelationId, action, 0);

                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };
                    }

                    var operatorResponse = JsonSerializer.Deserialize<OperatorRedCatBetResponseDTO>(responsePayload);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Success,
                            Balance = operatorResponse.Balance
                        }
                    };
                }
                catch (Exception ex)
                {
                    AddOperatorLog(request, "RESPONSE", endpointUrl, ex.Message, request.CorrelationId);

                    //await RetryBetPlacement(request, payload, endpointUrl, endpoint, request.CorrelationId, 0);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = ex.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Operator_Error
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    ex.Message,
                    ex.StackTrace
                });

                AddOperatorLog(request, "RESPONSE", endpointUrl, errorPayload, request.CorrelationId);

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Operator_Error
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessSettleBet(RedCatOperatorBetTransactionRequest request)
        {
            var path = $"/vs/bet/settle";
            var endpointUrl = $"{request.Endpoint}{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new
            {
                BetId = request.BetIdRef,
                MarktId = request.BetMarketId,
                GameId = request.GameId,
                BetStatusId = request.BetStatusId,
                BetStatusName = request.BetStatusName,
                Stake = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(endpointUrl).PostAsync(endpointUrl, httpContent);
                    var responsePayload = response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : response.ReasonPhrase;

                    AddOperatorLog(request, "RESPONSE", endpointUrl, responsePayload, request.CorrelationId);

                    if (!response.IsSuccessStatusCode)
                    {
                        //await RetryBetPlacement(request, payload, endpointUrl, request.CorrelationId, action, 0);

                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };
                    }

                    var operatorResponse = JsonSerializer.Deserialize<OperatorRedCatBetResponseDTO>(responsePayload);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Success,
                            Balance = operatorResponse.Balance
                        }
                    };
                }
                catch (Exception ex)
                {
                    AddOperatorLog(request, "RESPONSE", endpointUrl, ex.Message, request.CorrelationId);

                    //await RetryBetPlacement(request, payload, endpointUrl, endpoint, request.CorrelationId, 0);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = ex.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Operator_Error
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    ex.Message,
                    ex.StackTrace
                });

                AddOperatorLog(request, "RESPONSE", endpointUrl, errorPayload, request.CorrelationId);

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Operator_Error
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidPlacement(RedCatOperatorBetTransactionRequest request)
        {
            var path = $"/vs/bet/void/placement";
            var endpointUrl = $"{request.Endpoint}{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new
            {
                BetId = request.BetIdRef,
                MarktId = request.BetMarketId,
                GameId = request.GameId,
                BetStatusId = request.BetStatusId,
                BetStatusName = request.BetStatusName,
                Stake = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(endpointUrl).PostAsync(endpointUrl, httpContent);
                    var responsePayload = response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : response.ReasonPhrase;

                    AddOperatorLog(request, "RESPONSE", endpointUrl, responsePayload, request.CorrelationId);

                    if (!response.IsSuccessStatusCode)
                    {
                        //await RetryBetPlacement(request, payload, endpointUrl, request.CorrelationId, action, 0);

                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };
                    }

                    var operatorResponse = JsonSerializer.Deserialize<OperatorRedCatBetResponseDTO>(responsePayload);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Success,
                            Balance = operatorResponse.Balance
                        }
                    };
                }
                catch (Exception ex)
                {
                    AddOperatorLog(request, "RESPONSE", endpointUrl, ex.Message, request.CorrelationId);

                    //await RetryBetPlacement(request, payload, endpointUrl, endpoint, request.CorrelationId, 0);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = ex.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Operator_Error
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    ex.Message,
                    ex.StackTrace
                });

                AddOperatorLog(request, "RESPONSE", endpointUrl, errorPayload, request.CorrelationId);

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Operator_Error
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidSettlement(RedCatOperatorBetTransactionRequest request)
        {
            var path = $"/vs/bet/void/settle";
            var endpointUrl = $"{request.Endpoint}{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new
            {
                BetId = request.BetIdRef,
                MarktId = request.BetMarketId,
                GameId = request.GameId,
                BetStatusId = request.BetStatusId,
                BetStatusName = request.BetStatusName,
                Stake = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(endpointUrl).PostAsync(endpointUrl, httpContent);
                    var responsePayload = response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : response.ReasonPhrase;

                    AddOperatorLog(request, "RESPONSE", endpointUrl, responsePayload, request.CorrelationId);

                    if (!response.IsSuccessStatusCode)
                    {
                        //await RetryBetPlacement(request, payload, endpointUrl, request.CorrelationId, action, 0);

                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };
                    }

                    var operatorResponse = JsonSerializer.Deserialize<OperatorRedCatBetResponseDTO>(responsePayload);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.GetDescription(),
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Success,
                            Balance = operatorResponse.Balance
                        }
                    };
                }
                catch (Exception ex)
                {
                    AddOperatorLog(request, "RESPONSE", endpointUrl, ex.Message, request.CorrelationId);

                    //await RetryBetPlacement(request, payload, endpointUrl, endpoint, request.CorrelationId, 0);

                    return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                    {
                        StatusCode = StatusCode.Operator_Error,
                        Message = ex.Message,
                        Result = new RedCatBetTransactionResponseDTO()
                        {
                            StatusCode = (int)RedCatStatusCode.Operator_Error
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    ex.Message,
                    ex.StackTrace
                });

                AddOperatorLog(request, "RESPONSE", endpointUrl, errorPayload, request.CorrelationId);

                return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = StatusCode.Operator_Error,
                    Message = ex.Message,
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Operator_Error
                    }
                };
            }
        }

        private void AddOperatorLog(RedCatOperatorBetTransactionRequest bet, string direction, string endpointUrl, string payload, string correlationId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<PlatformContext>().UseSqlServer(_configuration["ConnectionStrings:Platform"]);

                using (var context = new PlatformContext(optionsBuilder.Options))
                {
                    context.OperatorLog.Add(new OperatorLog
                    {
                        Direction = direction,
                        ClientId = bet.ClientId,
                        ClientCode = bet.ClientCode,
                        URL = endpointUrl,
                        BetId = bet.BetId.ToString().ToUpper(),
                        Info = payload,
                        OperatorToken = bet.OperatorToken,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.UtcNow
                    });

                    context.SaveChanges();
                    scope.Complete();
                }
            }
        }

        private async Task RetryBetPlacement(RedCatOperatorBetTransactionRequest request, string payload, string endpoint, string correlationId, string action, int retries)
        {
            //if (retries == 0)
            //{
            //    AddOrUpdateOperatorSettlementRetry(request, endpoint, messageType, correlationId, false);
            //    return;
            //}

            retries -= 1;

            var path = "/bet/status/placement";
            var endpointUrl = $"{endpoint}{path}";

            try
            {
                AddOperatorLog(request, "REQUEST", endpointUrl, payload, correlationId);

                var response = await HttpClient(endpoint).PostAsync(endpointUrl, new StringContent(payload, Encoding.UTF8, "application/json"));
                var responsePayload = response.IsSuccessStatusCode ? response.Content.ReadAsStringAsync().Result : response.ReasonPhrase;

                AddOperatorLog(request, "RESPONSE", endpointUrl, responsePayload, correlationId);

                if (!response.IsSuccessStatusCode)
                {
                    await RetryBetPlacement(request, payload, endpoint, correlationId, action, retries);
                }
            }
            catch (Exception ex)
            {
                var errorPayload = JsonSerializer.Serialize(new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });

                AddOperatorLog(request, "RESPONSE", endpointUrl, errorPayload, correlationId);

                await RetryBetPlacement(request, payload, endpoint, correlationId, action, retries);
            }
        }
        private HttpClient HttpClient(string endpoint)
        {
            var client = _httpClients.Where(c => c.BaseAddress.OriginalString == endpoint).FirstOrDefault();

            if (client == null)
            {
                client = new HttpClient()
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                lock (_lock)
                    _httpClients.Add(client);
            }

            return client;
        }
    }
}

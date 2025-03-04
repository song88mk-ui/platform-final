using Microsoft.AspNetCore.Http;
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
        public OperatorRepository(IConfiguration configuration, PlatformContext platformContext)
        {
            _platformContext = platformContext;
            _configuration = configuration;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequest request)
        {
            var path = $"v1/vs/bet/placement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new OperatorRedCatBetRequestDTO()
            {
                BetId = request.BetId.ToString(),
                MarketId = request.BetMarketId,
                GameId = request.GameId,
                BetStatus = request.BetStatusName,
                //BetStatusId = request.BetStatusId,
                //BetStatusName = request.BetStatusName,
                UserId = request.MemberCode,
                Amount = request.Stake,
                Reason = string.Empty
            };

            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(request.Endpoint, request.OperatorToken, request.ClientId, request.IsAgent).PostAsync(path, httpContent);
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (operatorResponse == null)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };

                    if (operatorResponse.StatusId != 1000)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = operatorResponse.StatusId
                            }
                        };

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
            var path = $"v1/vs/bet/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new OperatorRedCatBetSettlementRequestDTO()
            {
                BetId = request.BetId.ToString(),
                MarketId = request.BetMarketId,
                GameId = request.GameId,
                BetStatus = request.BetStatusName,
                //BetStatusId = request.BetStatusId,
                //BetStatusName = request.BetStatusName,
                UserId = request.MemberCode,
                Payout = request.Payout,
                Amount = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(request.Endpoint, request.OperatorToken, request.ClientId, request.IsAgent).PostAsync(path, httpContent);
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (operatorResponse == null)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };

                    if (operatorResponse.StatusId != 1000)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = operatorResponse.StatusId
                            }
                        };

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
            var path = $"v1/vs/bet/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSVoidPlacement";

            var operatorRequest = new OperatorRedCatBetRequestDTO()
            {
                BetId = request.BetId.ToString(),
                MarketId = request.BetMarketId,
                GameId = request.GameId,
                BetStatus = request.BetStatusName,
                //BetStatusId = request.BetStatusId,
                //BetStatusName = request.BetStatusName,
                UserId = request.MemberCode,
                Amount = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(request.Endpoint, request.OperatorToken, request.ClientId, request.IsAgent).PostAsync(path, httpContent);
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (operatorResponse == null)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };

                    if (operatorResponse.StatusId != 1000)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = operatorResponse.StatusId
                            }
                        };

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
            var path = $"v1/vs/bet/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSVoidSettlement";

            var operatorRequest = new OperatorRedCatBetRequestDTO()
            {
                BetId = request.BetId.ToString(),
                MarketId = request.BetMarketId,
                GameId = request.GameId,
                BetStatus = request.BetStatusName,
                //BetStatusId = request.BetStatusId,
                //BetStatusName = request.BetStatusName,
                UserId = request.MemberCode,
                Amount = request.Stake,
                Reason = string.Empty
            };
            var payload = JsonSerializer.Serialize(operatorRequest, _jsonOptions);

            try
            {
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, request.CorrelationId);

                try
                {
                    var response = await HttpClient(request.Endpoint, request.OperatorToken, request.ClientId, request.IsAgent).PostAsync(path, httpContent);
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    if (operatorResponse == null)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = (int)RedCatStatusCode.Operator_Error
                            }
                        };
                    
                    if (operatorResponse.StatusId != 1000)
                        return new BaseResponseDTO<RedCatBetTransactionResponseDTO>()
                        {
                            StatusCode = StatusCode.Operator_Error,
                            Message = StatusCode.Operator_Error.GetDescription(),
                            Result = new RedCatBetTransactionResponseDTO()
                            {
                                StatusCode = operatorResponse.StatusId
                            }
                        };

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
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                AddOperatorLog(request, "REQUEST", endpointUrl, payload, correlationId);

                var response = await HttpClient(request.Endpoint, request.OperatorToken, request.ClientId, request.IsAgent).PostAsync(path, httpContent);
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
        private HttpClient HttpClient(string endpoint, string operatorToken, string clientId, bool isAgent)
        {
            lock (_lock)
            {
                var client = _httpClients.Where(c => c.BaseAddress.OriginalString == endpoint).FirstOrDefault();

                if (client == null)
                {
                    client = new HttpClient()
                    {
                        BaseAddress = new Uri(endpoint.TrimEnd('/')),
                        Timeout = TimeSpan.FromSeconds(5)
                    };

                    client.DefaultRequestHeaders.Add("x-is-agent", isAgent.ToString());
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                    client.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

                    _httpClients.Add(client);
                }

                return client;
            }
        }
    }
}

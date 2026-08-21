using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.MessageModels;
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
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptionsCaseInsensitive = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public readonly PlatformContext _platformContext;
        private readonly IConfiguration _configuration;

        private readonly object _lock = new object();
        private List<HttpClient> _httpClients = new List<HttpClient>();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();
        private readonly IRabbitMQRepository _rabbitMQRepository;
        public OperatorRepository(IConfiguration configuration, IRabbitMQRepository rabbitMQRepository, PlatformContext platformContext)
        {
            _rabbitMQRepository = rabbitMQRepository;
            _platformContext = platformContext;
            _configuration = configuration;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessBetPlacement(RedCatOperatorBetTransactionRequestDTO request)
        {
            var path = $"v1/bet/vs/placement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSBetPlacement";

            var operatorRequest = new OperatorRedCatBetRequestDTO()
            {
                BetId = request.BetId.ToString(),
                UserId = request.MemberCode,
                CurrencyCode = request.CurrencyCode,
                Amount = request.Stake,
                StakeTax = 0,
                WinningsTax = 0,
                ParlayBonus = 0,
                BonusId = string.Empty,
                BetStatus = request.BetStatusName,
                TransactionId = request.CorrelationId,
                TransactionDate = request.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = request.BetSelections.Select(betselection => new OperatorRedCatBetSelectionDTO()
                {
                    BetSelectionId = betselection.BetSelectionId,
                    GameId = betselection.GameId,
                    MarketId = betselection.BetMarketId,
                    BetSelectionStatus = betselection.BetSelectionStatus
                }).ToList()
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, _jsonOptionsCaseInsensitive);

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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = request.ClientId.ToString().ToUpper(),
                            UserId = request.MemberCode,
                            Balance = operatorResponse.Balance
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{request.ClientCode.ToString().ToLower()}"
                    });

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
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessSettleBet(RedCatOperatorBetTransactionRequestDTO request)
        {
            var path = $"v1/bet/vs/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSBetSettlement";

            var operatorRequest = new OperatorRedCatBetSettlementRequestDTO()
            {
                BetId = request.BetId.ToString(),
                UserId = request.MemberCode,
                CurrencyCode = request.CurrencyCode,
                Amount = request.Stake,
                Payout = request.Payout,
                StakeTax = 0,
                WinningsTax = 0,
                ParlayBonus = 0,
                BonusId = string.Empty,
                BetStatus = request.BetStatusName,
                TransactionId = request.CorrelationId,
                TransactionDate = request.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = request.BetSelections.Select(betselection => new OperatorRedCatBetSelectionDTO()
                {
                    BetSelectionId = betselection.BetSelectionId,
                    GameId = betselection.GameId,
                    MarketId = betselection.BetMarketId,
                    BetSelectionStatus = betselection.BetSelectionStatus
                }).ToList()
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, _jsonOptionsCaseInsensitive);

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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = request.ClientId.ToString().ToUpper(),
                            UserId = request.MemberCode,
                            Balance = operatorResponse.Balance
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{request.ClientCode.ToString().ToLower()}"
                    });

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
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidPlacement(RedCatOperatorBetTransactionRequestDTO request)
        {
            var path = $"v1/bet/vs/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSVoidPlacement";

            var operatorRequest = new OperatorRedCatBetSettlementRequestDTO()
            {
                BetId = request.BetId.ToString(),
                UserId = request.MemberCode,
                CurrencyCode = request.CurrencyCode,
                Amount = request.Stake,
                Payout = request.Payout,
                StakeTax = 0,
                WinningsTax = 0,
                ParlayBonus = 0,
                BonusId = string.Empty,
                BetStatus = request.BetStatusName,
                TransactionId = request.CorrelationId,
                TransactionDate = request.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = request.BetSelections.Select(betselection => new OperatorRedCatBetSelectionDTO()
                {
                    BetSelectionId = betselection.BetSelectionId,
                    GameId = betselection.GameId,
                    MarketId = betselection.BetMarketId,
                    BetSelectionStatus = betselection.BetSelectionStatus
                }).ToList()
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, _jsonOptionsCaseInsensitive);

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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = request.ClientId.ToString().ToUpper(),
                            UserId = request.MemberCode,
                            Balance = operatorResponse.Balance
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{request.ClientCode.ToString().ToLower()}"
                    });

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
        public async Task<BaseResponseDTO<RedCatBetTransactionResponseDTO>> ProcessVoidSettlement(RedCatOperatorBetTransactionRequestDTO request)
        {
            var path = $"v1/bet/vs/settlement";
            var endpointUrl = $"{request.Endpoint}/{path}";

            var action = "VSVoidSettlement";

            var operatorRequest = new OperatorRedCatBetSettlementRequestDTO()
            {
                BetId = request.BetId.ToString(),
                UserId = request.MemberCode,
                CurrencyCode = request.CurrencyCode,
                Amount = request.Stake,
                Payout = request.Payout,
                StakeTax = 0,
                WinningsTax = 0,
                ParlayBonus = 0,
                BonusId = string.Empty,
                BetStatus = request.BetStatusName,
                TransactionId = request.CorrelationId,
                TransactionDate = request.BetDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffff"),
                BetSelections = request.BetSelections.Select(betselection => new OperatorRedCatBetSelectionDTO()
                {
                    BetSelectionId = betselection.BetSelectionId,
                    GameId = betselection.GameId,
                    MarketId = betselection.BetMarketId,
                    BetSelectionStatus = betselection.BetSelectionStatus
                }).ToList()
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

                    var operatorResponse = JsonSerializer.Deserialize<OperatorBetResponseDTO>(responsePayload, _jsonOptionsCaseInsensitive);

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

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(new OnWalletChange
                        {
                            ClientId = request.ClientId.ToString().ToUpper(),
                            UserId = request.MemberCode,
                            Balance = operatorResponse.Balance
                        }),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnWalletChange",
                        RoutingKey = $"r.wallet.{request.ClientCode.ToString().ToLower()}"
                    });

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

        private void AddOperatorLog(RedCatOperatorBetTransactionRequestDTO bet, string direction, string endpointUrl, string payload, string correlationId)
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

        private async Task RetryBetPlacement(RedCatOperatorBetTransactionRequestDTO request, string payload, string endpoint, string correlationId, string action, int retries)
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

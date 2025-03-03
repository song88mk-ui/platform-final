using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.RedCat.API.Models;
using Platform.RedCat.API.Telemetry;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using Platform.Shared.Models;
using Platform.Shared.Models.RedCat;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WalletBalanceResponseDTO = Platform.RedCat.API.Models.WalletBalanceResponseDTO;

namespace Platform.RedCat.API.Repositories
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly IMemCache _memCache;
        private readonly PlatformContext _platformContext;
        private readonly IBettingRepository _bettingRepository;
        private readonly IOperatorRepository _operatorRepository;
        private readonly string _sharedKey;

        private readonly ActivitySource _activitySource;

        public PlatformRepository(IMemCache memCache, PlatformContext platformContext, IOperatorRepository operatorRepository, IBettingRepository bettingRepository, IConfiguration configuration, Instrumentation instrumentation)
        {
            _memCache = memCache;
            _platformContext = platformContext;
            _bettingRepository = bettingRepository;
            _operatorRepository = operatorRepository;
            _sharedKey = configuration["Custom:SharedKey"];

            _activitySource = instrumentation.ActivitySource;
        }

        public async Task<RedCatBaseResponseDTO<ValidateTokenResponseDTO>> ValidateToken(ValidateTokenRequestDTO request)
        {
            using var activity = _activitySource.StartActivity("vs.validate");

            activity?.SetTag("operator.token", request.AuthToken);
            activity?.SetTag("security.key", request.Security);


            activity?.AddEvent(new ActivityEvent("validate.security.key"));
            if (!IsValidSecurity<ValidateTokenRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;

                return ReturnErrorBetResponse<ValidateTokenResponseDTO>(statusCode, new ValidateTokenResponseDTO()
                {
                    StatusCode = (int)statusCode
                }, activity);
            }

            activity?.AddEvent(new ActivityEvent("validate.auth"));
            if (request.AuthToken.Count(c => c == ':') != 1)
            {
                var statusCode = RedCatStatusCode.Unknown_Operator_Id;

                return ReturnErrorBetResponse<ValidateTokenResponseDTO>(statusCode, new ValidateTokenResponseDTO()
                {
                    StatusCode = (int)statusCode
                }, activity);
            }

            var extractedToken = request.AuthToken.Split(":");
            var clientId = extractedToken.FirstOrDefault();
            var operatorToken = extractedToken.LastOrDefault();

            activity?.AddEvent(new ActivityEvent("validate.client"));
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                var statusCode = RedCatStatusCode.Unknown_Operator_Id;

                return ReturnErrorBetResponse<ValidateTokenResponseDTO>(statusCode, new ValidateTokenResponseDTO()
                {
                    StatusCode = (int)statusCode
                }, activity);
            }

            activity?.AddEvent(new ActivityEvent("validate.auth.token"));
            var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
            {
                return ReturnErrorBetResponse<ValidateTokenResponseDTO>(authenticationResponse.StatusCode, new ValidateTokenResponseDTO()
                {
                    StatusCode = (int)authenticationResponse.StatusCode
                }, activity);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return new RedCatBaseResponseDTO<ValidateTokenResponseDTO>
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new ValidateTokenResponseDTO()
                {
                    StatusCode = (int)authenticationResponse.StatusCode,
                    MemberCode = authenticationResponse.Result.UserId,
                    OperatorId = clientResponse.Result.ClientId.ToString(),
                    Currency = Formatting.GetCurrencyCode(authenticationResponse.Result.CurrencyCode).ToString()
                }
            };
        }

        public async Task<RedCatBaseResponseDTO<WalletBalanceResponseDTO>> GetWalletBalance(WalletBalanceRequestDTO request, bool isInternalCall = false)
        {
            using var activity = _activitySource.StartActivity("vs.validate");

            activity?.SetTag("operator.token", request.AuthToken);
            activity?.SetTag("security.key", request.Security);
            activity?.SetTag("currency.code", request.Currency);

            var result = new WalletBalanceResponseDTO()
            {
                Balance = 0
            };

            try
            {
                activity?.AddEvent(new ActivityEvent("validate.security.key"));
                if (!isInternalCall && !IsValidSecurity<WalletBalanceRequestDTO>(request, request.Security))
                {
                    var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                    return ReturnErrorBetResponse<WalletBalanceResponseDTO>(statusCode, result, activity);
                }

                activity?.AddEvent(new ActivityEvent("validate.auth"));
                if (request.AuthToken.Count(c => c == ':') != 1)
                    return ReturnErrorBetResponse<WalletBalanceResponseDTO>(RedCatStatusCode.Unknown_Operator_Id, result, activity);

                var extractedToken = request.AuthToken.Split(":");
                var clientId = extractedToken.FirstOrDefault();
                var operatorToken = extractedToken.LastOrDefault();

                activity?.AddEvent(new ActivityEvent("validate.client"));
                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                    return ReturnErrorBetResponse<WalletBalanceResponseDTO>(RedCatStatusCode.Unknown_Operator_Id, result, activity);

                activity?.AddEvent(new ActivityEvent("validate.auth.token"));
                var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
                    return ReturnErrorBetResponse<WalletBalanceResponseDTO>(RedCatStatusCode.Operator_Error, result, activity);

                //if(authenticationResponse.Result.UserId != request.MemberCode)
                //{
                //    return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                //    {
                //        StatusCode = RedCatStatusCode.Unknown_Member_Code,
                //        Message = RedCatStatusCode.Unknown_Member_Code.GetDescription(),
                //        Result = new RedCatBetTransactionResponseDTO()
                //        {
                //            StatusCode = (int)RedCatStatusCode.Unknown_Member_Code
                //        }
                //    };
                //}

                activity?.SetStatus(ActivityStatusCode.Ok);
                return new RedCatBaseResponseDTO<WalletBalanceResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Success,
                    Message = StatusCode.Success.GetDescription(),
                    Result = new WalletBalanceResponseDTO()
                    {
                        Balance = authenticationResponse.Result.Balance
                    }
                };
            }
            catch (Exception ex)
            {
                return ReturnErrorBetResponse<WalletBalanceResponseDTO>(RedCatStatusCode.Error, result, activity, ex.Message);
            }
        }

        public async Task<RedCatBaseResponseDTO<ApprovalResponseDTO>> GetApproval(ApprovalRequestDTO request, bool isInternalCall = false)
        {
            if (!isInternalCall && !IsValidSecurity<ApprovalRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                return new RedCatBaseResponseDTO<ApprovalResponseDTO>
                {
                    StatusCode = statusCode,
                    Message = statusCode.GetDescription(),
                    Result = new ApprovalResponseDTO()
                    {
                        StatusCode = (int)statusCode
                    }
                };
            }

            var walletBalance = await GetWalletBalance(new WalletBalanceRequestDTO()
            {
                AuthToken = request.AuthToken,
                Security = request.Security,
                Currency = request.Currency,
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId
            }, true);

            if (walletBalance.StatusCode != RedCatStatusCode.Success)
                return new RedCatBaseResponseDTO<ApprovalResponseDTO>()
                {
                    StatusCode = walletBalance.StatusCode,
                    Message = walletBalance.Message,
                    Result = new ApprovalResponseDTO()
                    {
                        StatusCode = (int)walletBalance.StatusCode
                    }
                };

            if (walletBalance.Result.Balance < request.TransactionAmt)
                return new RedCatBaseResponseDTO<ApprovalResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Insufficient_Funds,
                    Message = RedCatStatusCode.Insufficient_Funds.GetDescription(),
                    Result = new ApprovalResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Insufficient_Funds
                    }
                };

            return new RedCatBaseResponseDTO<ApprovalResponseDTO>()
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new ApprovalResponseDTO()
                {
                    StatusCode = (int)RedCatStatusCode.Success
                }
            };
        }

        public async Task<RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>> Stake(RedCatBetTransactionRequestDTO request)
        {
            if (!IsValidSecurity<RedCatBetTransactionRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = statusCode,
                    Message = statusCode.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)statusCode
                    }
                };
            }

            var extractedToken = request.AuthToken.Split(":");
            var clientId = extractedToken.FirstOrDefault();
            var operatorToken = extractedToken.LastOrDefault();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Unknown_Operator_Id
                    }
                };

            var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = authenticationResponse.Result.StatusId
                    }
                };

            //if(authenticationResponse.Result.UserId != request.MemberCode)
            //{
            //    return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            //    {
            //        StatusCode = RedCatStatusCode.Unknown_Member_Code,
            //        Message = RedCatStatusCode.Unknown_Member_Code.GetDescription(),
            //        Result = new RedCatBetTransactionResponseDTO()
            //        {
            //            StatusCode = (int)RedCatStatusCode.Unknown_Member_Code
            //        }
            //    };
            //}

            var rate = await GetExchangeRate(authenticationResponse.Result.CurrencyCode);

            var bettingResponse = await _bettingRepository.PlaceBet(new RedCatBetRequest()
            {
                BetId = request.BetId,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientCode = clientResponse.Result.ClientCode,
                Endpoint = clientResponse.Result.Endpoint,
                OperatorToken = operatorToken,
                ExhangeRate = rate.ExchangeRate,
                CurrencyCode = Formatting.GetCurrencyCode(authenticationResponse.Result.CurrencyCode),
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId,
                EventDate = request.EventDate,
                BetMarketId = request.BetMarketId,
                TransactionAmt = request.TransactionAmt,
                TransactionId = request.TransactionId,
                GameId = request.GameId,
            });

            return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new RedCatBetTransactionResponseDTO()
                {
                    StatusCode = (int)RedCatStatusCode.Success,
                    Balance = bettingResponse.Result.Balance,
                    TransactionRefId = bettingResponse.Result.TransactionRefId,
                }
            };
        }

        public async Task<RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>> Settle(RedCatBetTransactionRequestDTO request)
        {
            if (!IsValidSecurity<RedCatBetTransactionRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = statusCode,
                    Message = statusCode.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)statusCode
                    }
                };
            }

            var extractedToken = request.AuthToken.Split(":");
            var clientId = extractedToken.FirstOrDefault();
            var operatorToken = extractedToken.LastOrDefault();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Unknown_Operator_Id
                    }
                };

            var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = authenticationResponse.Result.StatusId
                    }
                };

            //if(authenticationResponse.Result.UserId != request.MemberCode)
            //{
            //    return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            //    {
            //        StatusCode = RedCatStatusCode.Unknown_Member_Code,
            //        Message = RedCatStatusCode.Unknown_Member_Code.GetDescription(),
            //        Result = new RedCatBetTransactionResponseDTO()
            //        {
            //            StatusCode = (int)RedCatStatusCode.Unknown_Member_Code
            //        }
            //    };
            //}

            var bettingResponse = await _bettingRepository.SettleBet(new RedCatBetRequest()
            {
                BetId = request.BetId,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientCode = clientResponse.Result.ClientCode,
                Endpoint = clientResponse.Result.Endpoint,
                OperatorToken = operatorToken,
                CurrencyCode = Formatting.GetCurrencyCode(authenticationResponse.Result.CurrencyCode),
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId,
                EventDate = request.EventDate,
                BetMarketId = request.BetMarketId,
                TransactionAmt = request.TransactionAmt,
                TransactionId = request.TransactionId,
                GameId = request.GameId,
            });

            return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new RedCatBetTransactionResponseDTO()
                {
                    StatusCode = (int)RedCatStatusCode.Success,
                    Balance = bettingResponse.Result.Balance,
                    TransactionRefId = bettingResponse.Result.TransactionRefId
                }
            };
        }
        public async Task<RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidStake(RedCatVoidBetTransactionRequestDTO request)
        {
            if (!IsValidSecurity<RedCatBetTransactionRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = statusCode,
                    Message = statusCode.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)statusCode
                    }
                };
            }

            var extractedToken = request.AuthToken.Split(":");
            var clientId = extractedToken.FirstOrDefault();
            var operatorToken = extractedToken.LastOrDefault();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Unknown_Operator_Id
                    }
                };

            var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = authenticationResponse.Result.StatusId
                    }
                };

            //if(authenticationResponse.Result.UserId != request.MemberCode)
            //{
            //    return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            //    {
            //        StatusCode = RedCatStatusCode.Unknown_Member_Code,
            //        Message = RedCatStatusCode.Unknown_Member_Code.GetDescription(),
            //        Result = new RedCatBetTransactionResponseDTO()
            //        {
            //            StatusCode = (int)RedCatStatusCode.Unknown_Member_Code
            //        }
            //    };
            //}

            var bettingResponse = await _bettingRepository.VoidStake(new RedCatVoidBetRequest()
            {
                BetId = request.BetId,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientCode = clientResponse.Result.ClientCode,
                Endpoint = clientResponse.Result.Endpoint,
                OperatorToken = operatorToken,
                CurrencyCode = Formatting.GetCurrencyCode(authenticationResponse.Result.CurrencyCode),
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId,
                EventDate = request.EventDate,
                BetMarketId = request.BetMarketId,
                TransactionAmt = request.TransactionAmt,
                TransactionId = request.TransactionId,
                GameId = request.GameId,
                RollbackTransactionId = request.RollbackTransactionId
            });

            return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new RedCatBetTransactionResponseDTO()
                {
                    StatusCode = (int)RedCatStatusCode.Success,
                    Balance = bettingResponse.Result.Balance,
                    TransactionRefId = bettingResponse.Result.TransactionRefId
                }
            };
        }
        public async Task<RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>> VoidSettle(RedCatVoidBetTransactionRequestDTO request)
        {
            if (!IsValidSecurity<RedCatBetTransactionRequestDTO>(request, request.Security))
            {
                var statusCode = RedCatStatusCode.Invalid_Security_Hash;
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>
                {
                    StatusCode = statusCode,
                    Message = statusCode.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)statusCode
                    }
                };
            }

            var extractedToken = request.AuthToken.Split(":");
            var clientId = extractedToken.FirstOrDefault();
            var operatorToken = extractedToken.LastOrDefault();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = (int)RedCatStatusCode.Unknown_Operator_Id
                    }
                };

            var authenticationResponse = await _operatorRepository.Authenticate(clientResponse.Result.ClientId.ToString(), operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            if (authenticationResponse.StatusCode != RedCatStatusCode.Success)
                return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
                {
                    StatusCode = RedCatStatusCode.Unknown_Operator_Id,
                    Message = RedCatStatusCode.Unknown_Operator_Id.GetDescription(),
                    Result = new RedCatBetTransactionResponseDTO()
                    {
                        StatusCode = authenticationResponse.Result.StatusId
                    }
                };

            //if(authenticationResponse.Result.UserId != request.MemberCode)
            //{
            //    return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            //    {
            //        StatusCode = RedCatStatusCode.Unknown_Member_Code,
            //        Message = RedCatStatusCode.Unknown_Member_Code.GetDescription(),
            //        Result = new RedCatBetTransactionResponseDTO()
            //        {
            //            StatusCode = (int)RedCatStatusCode.Unknown_Member_Code
            //        }
            //    };
            //}

            var bettingResponse = await _bettingRepository.VoidSettle(new RedCatVoidBetRequest()
            {
                BetId = request.BetId,
                ClientId = clientResponse.Result.ClientId.ToString().ToUpper(),
                ClientCode = clientResponse.Result.ClientCode,
                Endpoint = clientResponse.Result.Endpoint,
                OperatorToken = operatorToken,
                CurrencyCode = Formatting.GetCurrencyCode(authenticationResponse.Result.CurrencyCode),
                MemberCode = request.MemberCode,
                OperatorId = request.OperatorId,
                EventDate = request.EventDate,
                BetMarketId = request.BetMarketId,
                TransactionAmt = request.TransactionAmt,
                TransactionId = request.TransactionId,
                GameId = request.GameId,
                RollbackTransactionId = request.RollbackTransactionId
            });

            return new RedCatBaseResponseDTO<RedCatBetTransactionResponseDTO>()
            {
                StatusCode = RedCatStatusCode.Success,
                Message = RedCatStatusCode.Success.GetDescription(),
                Result = new RedCatBetTransactionResponseDTO()
                {
                    StatusCode = (int)RedCatStatusCode.Success,
                    Balance = bettingResponse.Result.Balance,
                    TransactionRefId = bettingResponse.Result.TransactionRefId
                }
            };
        }

        private bool IsValidSecurity<T>(T request, string providedSecurity)
        {
            if (string.IsNullOrEmpty(providedSecurity))
                return false;

            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(request));
            parameters.Remove("Security");
            var sortedValues = parameters.OrderBy(p => p.Key).Select(c => c.Value);

            string dataString = string.Concat(sortedValues);
            string expectedSecurity = GenerateHMACSHA256(dataString, _sharedKey);

            return expectedSecurity == providedSecurity;
        }
        private string GenerateHMACSHA256(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }
        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                var key = $"cc:{guid.ToString().ToUpper()}";

                var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

                if (cache != null)
                    return cache;

                var result = await _platformContext.Client.FindAsync(guid);

                if (result == null || result.IsActive == false)
                {
                    return new BaseResponseDTO<ClientResponse>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                    };
                }
                else
                {
                    var finalResult = new BaseResponseDTO<ClientResponse>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace('_', ' '),
                        Result = new ClientResponse
                        {
                            ClientId = result.ClientId,
                            ClientName = result.ClientName,
                            ClientCode = result.ClientCode,
                            LimitId = result.LimitId,
                            IsActive = result.IsActive,
                            Endpoint = result.Endpoint,
                            CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                            ClientIdRef = result.ClientIdRef,
                            IsAgent = result.IsAgent
                        }
                    };

                    _memCache.AddOrUpdateCache(key, finalResult, 24);

                    return finalResult;
                }
            }
            else
            {
                //TODO: temp approach while using the old backoffice sportsbook client with ClientId as integer type
                var isInteger = int.TryParse(clientId, out int intClientId);

                if (isInteger)
                {
                    var key = $"cc:{intClientId.ToString().ToUpper()}";

                    var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

                    if (cache != null)
                        return cache;

                    var result = await _platformContext.Client.Where(c => c.ClientIdRef == intClientId).FirstOrDefaultAsync();

                    if (result == null || result.IsActive == false)
                    {
                        return new BaseResponseDTO<ClientResponse>
                        {
                            StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                            Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                        };
                    }
                    else
                    {
                        var finalResult = new BaseResponseDTO<ClientResponse>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString().Replace('_', ' '),
                            Result = new ClientResponse
                            {
                                ClientId = result.ClientId,
                                ClientName = result.ClientName,
                                ClientCode = result.ClientCode,
                                LimitId = result.LimitId,
                                IsActive = result.IsActive,
                                Endpoint = result.Endpoint,
                                CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                                ClientIdRef = result.ClientIdRef,
                                IsAgent = result.IsAgent
                            }
                        };

                        _memCache.AddOrUpdateCache(key, finalResult, 24);

                        return finalResult;
                    }
                }
                else
                {
                    return new BaseResponseDTO<ClientResponse>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                    };
                }
            }
        }


        private async Task<ForexEURResponse> GetExchangeRate(string currencyCode)
        {
            if (currencyCode.ToUpper() == "USDT")
                currencyCode = "USD";

            var result = await _platformContext.ForexEUR.FindAsync(currencyCode);

            if (result == null)
                return new ForexEURResponse();

            return new ForexEURResponse
            {
                CurrencyCode = result.CurrencyCode,
                ExchangeRate = result.ExchangeRate
            };
        }
        private RedCatBaseResponseDTO<T> ReturnErrorBetResponse<T>(RedCatStatusCode statusCode, T result, Activity activity, string message = "")
        {
            activity?.SetStatus(ActivityStatusCode.Error, statusCode.ToString());

            return new RedCatBaseResponseDTO<T>
            {
                StatusCode = statusCode,
                Message = !string.IsNullOrEmpty(message) ? message : statusCode.GetDescription(),
                Result = result
            };
        }
    }
}

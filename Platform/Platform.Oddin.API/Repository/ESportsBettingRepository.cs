using Microsoft.Extensions.Configuration;
using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Oddin.API.Repository
{
    public class ESportsBettingRepository : IESportsBettingRepository
    {
        private readonly IConfiguration _configuration;
        private readonly IMemCache _memCache;

        private readonly IOperatorRepository _operatorRepository;
        private readonly PlatformContext _platformContext;

        public ESportsBettingRepository(IConfiguration configuration, IOperatorRepository operatorRepository, PlatformContext platformContext, IMemCache memCache)
        {
            _configuration = configuration;
            _memCache = memCache;
            _operatorRepository = operatorRepository;

            _platformContext = platformContext;
        }

        public async Task<Tuple<SessionCode, UserDetailsResponse>> GetUserDetails(UserDetailsRequest request)
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return new Tuple<SessionCode, UserDetailsResponse>(SessionCode.INVALID_TOKEN, new UserDetailsResponse());
            }

            if (request.Token.Contains('|') == false)
            {
                return new Tuple<SessionCode, UserDetailsResponse>(SessionCode.INVALID_TOKEN, new UserDetailsResponse());
            }

            var split = request.Token.Split('|');
            var clientId = split.First();
            var operatorToken = split.Last();

            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new Tuple<SessionCode, UserDetailsResponse>(SessionCode.INVALID_TOKEN, new UserDetailsResponse());
            }

            var authenticationResponse = await _operatorRepository.Authenticate(clientId, operatorToken, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

            return new Tuple<SessionCode, UserDetailsResponse>(SessionCode.OK, new UserDetailsResponse
            {
                UserId = authenticationResponse.Result.UserId,
                Currency = authenticationResponse.Result.CurrencyCode,
                ErrorCode = (int)SessionCode.OK,
                Language = authenticationResponse.Result.LanguageCode
            });
        }

        public async Task<CreditUserResponse> CreditUser(CreditUserRequest request)
        {
            try
            {
                // Validate Request
                var clientId = "";
                var endpoint = "";
                var operatorResponse = _operatorRepository.Authenticate(clientId, request.Token, endpoint, false);

                // Wallet Reduce

                // Validations

                // Response

                return new CreditUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.OK
                };
            }
            catch (Exception)
            {
                return new CreditUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.CUSTOMER_ERROR
                };
            }
        }

        public async Task<DebitUserResponse> DebitUser(DebitUserRequest request)
        {
            try
            {
                // Check Transaction in database

                if (string.IsNullOrEmpty(request.Token))
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.INVALID_TOKEN };

                if (request.Token.Contains('|') == false)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.INVALID_TOKEN };

                var split = request.Token.Split('|');
                var clientId = split.First();
                var operatorToken = split.Last();

                var clientResponse = await GetClient(clientId);

                if (clientResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.INVALID_TOKEN };

                var authenticationResponse = await _operatorRepository.Authenticate(clientId, request.Token, clientResponse.Result.Endpoint, clientResponse.Result.IsAgent);

                if (authenticationResponse.StatusCode != StatusCode.Success)
                    return new DebitUserResponse() { OperatorTransactionId = request.TransactionId, ErrorCode = (int)SessionCode.INVALID_TOKEN };

                // Wallet Reduce

                var isBetExist = false;

                if (isBetExist)
                {
                }


                // Validations

                // Response

                return new DebitUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.OK
                };
            }
            catch (Exception)
            {
                return new DebitUserResponse()
                {
                    OperatorTransactionId = request.TransactionId,
                    ErrorCode = (int)SessionCode.CUSTOMER_ERROR
                };
            }
        }

        #region Utilities
        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var key = $"cc:{clientId.ToUpper()}";

            var cache = _memCache.GetCache<BaseResponseDTO<ClientResponse>>(key);

            if (cache != null)
                return cache;

            var result = await _platformContext.Client.FindAsync(Guid.Parse(clientId));

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
        #endregion
    }
}

using Platform.Shared.Data.Platform;
using Platform.Shared.Data.Platform.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Betting.API.Repository
{
    public class ClientRepository : IClientRepository
    {
        private readonly PlatformContext _platformContext;
        public ClientRepository(PlatformContext platformContext)
        {
            _platformContext = platformContext;
        }

        public async Task<BaseResponseDTO<StatusCode>> InsertClient(ClientRequestDTO request)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    _platformContext.Client.Add(new Client
                    {
                        ClientId = request.ClientId,
                        ClientCode = request.ClientCode,
                        ClientName = request.ClientName,
                        ClientIdRef = request.ClientIdRef,
                        CurrentCompanyShare = request.CurrentCompanyShare,
                        EffectiveDate = request.EffectiveDate,
                        IsActive = request.IsActive,
                        Endpoint = request.Endpoint,
                        LimitId = request.LimitId,
                        NextCompanyShare = request.NextCompanyShare
                    });
                    await _platformContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch
                {
                    scope.Dispose();
                    return new BaseResponseDTO<StatusCode>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString()
                    };
                }
            }
            return new BaseResponseDTO<StatusCode>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }
    }
}
 

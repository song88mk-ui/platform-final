using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Betting.API.Repository
{
    public class ClientBetConfigRepository : IClientBetConfigRepository
    {
        private readonly PlatformContext _platformContext;
        private readonly IMessageBroker _messageBroker;
        public ClientBetConfigRepository(PlatformContext platformContext, IMessageBroker messageBroker)
        {
            _platformContext = platformContext;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }

        public async Task<BaseResponseDTO<StatusCode>> ProcessClientBetConfig(BackOfficeBetConfigPayloadDTO request)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    foreach (var betconfig in request.BetConfigPayload)
                    {
                        _platformContext.BetConfig.Add(betconfig);
                    }

                    await _platformContext.SaveChangesAsync();

                    scope.Complete();                    
                }
                catch (Exception ex)
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
 

using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
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

        public async Task<BaseResponseDTO<StatusCode>> ProcessClientBetConfig(BackOfficeBetConfigPayloadDTO payload)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var clientID = payload.ClientId;
                    foreach (var config in payload.BetConfigPayload)
                    {
                        var betconfig = _platformContext.BetConfig.FirstOrDefault(b => b.ClientId == clientID && b.CurrencyCode == config.CurrencyCode);
                        if (betconfig != null)
                        {
                            betconfig.OverrideConfig = "";
                            betconfig.ParlayConfig = "";

                            _platformContext.BetConfig.Update(betconfig);
                        }
                        else
                        {
                            _platformContext.BetConfig.Add(new BetConfig
                            {
                                ClientId = clientID,
                                CurrencyCode = config.CurrencyCode,
                                StakeLimitFixture = 0, //TODO
                                MaxParlayPayout = config.Parlay.MaxPayout,
                                DefaultConfig = "",
                                OverrideConfig = "",
                                ParlayConfig = ""
                            });
                        }
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
 

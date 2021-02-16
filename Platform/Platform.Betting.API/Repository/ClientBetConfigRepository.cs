using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                    var defaultJSON = JsonSerializer.Serialize(
                        new DefaultSportConfig
                        {
                            MinStake = 1,
                            MaxStake = 100
                        });
                    foreach (var config in payload.BetConfigPayload)
                    {
                        var overridesJSON = JsonSerializer.Serialize(new List<OverrideSportConfig>
                        {
                            new OverrideSportConfig
                            {
                                SportId = 1,
                                SportName = "Soccer",
                                MinStake = config.Football.MinBet,
                                MaxStake = config.Football.MaxBet
                            },
                            new OverrideSportConfig
                            {
                                SportId = 2,
                                SportName = "Cricket",
                                MinStake = config.Cricket.MinBet,
                                MaxStake = config.Cricket.MaxBet
                            },
                            new OverrideSportConfig
                            {
                                SportId = 3,
                                SportName = "Other Sports",
                                MinStake = config.OtherSports.MinBet,
                                MaxStake = config.OtherSports.MaxBet
                            },
                        });

                        var parleyJSON = JsonSerializer.Serialize(new ParlayConfig
                        {
                            MinStake = config.Parlay.MinBet,
                            MaxStake = config.Parlay.MaxBet
                        });

                        var betconfig = _platformContext.BetConfig.FirstOrDefault(b => b.ClientId == clientID && b.CurrencyCode == config.CurrencyCode);
                        if (betconfig != null)
                        {
                            

                            betconfig.OverrideConfig = overridesJSON;
                            betconfig.ParlayConfig = parleyJSON;

                            _platformContext.BetConfig.Update(betconfig);
                        }
                        else
                        {
                            _platformContext.BetConfig.Add(new BetConfig
                            {
                                ClientId = clientID,
                                CurrencyCode = config.CurrencyCode,
                                StakeLimitFixture = 0, //TODO: remove???
                                MaxParlayPayout = config.Parlay.MaxPayout,
                                DefaultConfig = defaultJSON,
                                OverrideConfig = overridesJSON,
                                ParlayConfig = parleyJSON
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
                        Message = ex.Message
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
 

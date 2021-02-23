using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Betting;
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
    public class BetConfigRepository : IBetConfigRepository
    {
        private readonly BettingContext _bettingContext;
        private readonly IMessageBroker _messageBroker;
        public BetConfigRepository(BettingContext bettingContext, IMessageBroker messageBroker)
        {
            _bettingContext = bettingContext;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }

        public async Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO payload)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var clientID = payload.ClientId;                    
                    var dateCreated = DateTime.UtcNow;

                    foreach (var config in payload.BetConfigPayload)
                    {
                        var defaultJSON = JsonSerializer.Serialize(new DefaultSportConfig
                        {
                            MinStake = config.OtherSports.MinBet,
                            MaxStake = config.OtherSports.MaxBet,
                            StakeLimitFixture = config.OtherSports.MaxFixtureBet
                        });
                        var overrideJSON = JsonSerializer.Serialize(new List<OverrideSportConfig>
                        {
                            new OverrideSportConfig
                            {
                                SportId = 1,
                                SportName = "Soccer",
                                MinStake = config.Football.MinBet,
                                MaxStake = config.Football.MaxBet,
                                StakeLimitFixture = config.Football.MaxFixtureBet
                            },
                            new OverrideSportConfig
                            {
                                SportId = 2,
                                SportName = "Cricket",
                                MinStake = config.Cricket.MinBet,
                                MaxStake = config.Cricket.MaxBet,
                                StakeLimitFixture = config.Cricket.MaxFixtureBet
                            }
                        });
                        var parleyJSON = JsonSerializer.Serialize(new ParlayConfig
                        {
                            MinStake = config.Parlay.MinBet,
                            MaxStake = config.Parlay.MaxBet,
                            MaxPayout = config.Parlay.MaxPayout,
                        });

                        var betconfig = _bettingContext.BetConfig.FirstOrDefault(b => b.ClientId == clientID && b.CurrencyCode == config.CurrencyCode);
                        if (betconfig != null)
                        {
                            betconfig.DefaultConfig = defaultJSON;
                            betconfig.OverrideConfig = overrideJSON;
                            betconfig.ParlayConfig = parleyJSON;
                            betconfig.CreatedDate = dateCreated;

                            _bettingContext.BetConfig.Update(betconfig);
                        }
                        else
                        {
                            _bettingContext.BetConfig.Add(new BetConfig
                            {
                                ClientId = clientID,
                                CurrencyCode = config.CurrencyCode,
                                DefaultConfig = defaultJSON,
                                OverrideConfig = overrideJSON,
                                ParlayConfig = parleyJSON,
                                CreatedDate = dateCreated
                            });
                        }
                    }

                    await _bettingContext.SaveChangesAsync();

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
 

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
                    foreach (var config in payload.BetConfigPayload)
                    {
                        var betconfig = _platformContext.BetConfig.FirstOrDefault(b => b.ClientId == config.ClientId && b.SportsId == config.SportsId && b.CurrencyCode == config.CurrencyCode);
                        if (betconfig != null)
                        {
                            betconfig.MinBet = config.MinBet;
                            betconfig.MaxBet = config.MaxBet;
                            betconfig.MaxPerGame = config.MaxPerGame;
                            betconfig.TransactionDate = DateTime.UtcNow;

                            _platformContext.BetConfig.Update(betconfig);
                        }
                        else
                        {
                            var newbetconfig = new BetConfig
                            {
                                ClientId = config.ClientId,
                                SportsId = config.SportsId,
                                CategoryId = config.CategoryId,
                                ReferenceBet = config.ReferenceBet,
                                MinBet = config.MinBet,
                                MaxBet = config.MaxBet,
                                MaxPerGame = config.MaxPerGame,
                                CompanyShare = config.CompanyShare,
                                TransactionDate = config.TransactionDate,
                                EffectivityDate = config.EffectivityDate,
                                CurrencyCode = config.CurrencyCode,
                            };

                            _platformContext.BetConfig.Add(newbetconfig);
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
 

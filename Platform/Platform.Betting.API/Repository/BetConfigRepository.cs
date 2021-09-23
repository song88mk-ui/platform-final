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
        public BetConfigRepository(BettingContext bettingContext)
        {
            _bettingContext = bettingContext;
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
                                SportId = 21,
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

                        var taxJSON = JsonSerializer.Serialize(config.TaxConfig);

                        var betconfig = _bettingContext.BetConfig.FirstOrDefault(b => b.ClientId == clientID && b.CurrencyCode == config.CurrencyCode);
                        if (betconfig != null)
                        {
                            betconfig.DefaultConfig = defaultJSON;
                            betconfig.OverrideConfig = overrideJSON;
                            betconfig.ParlayConfig = parleyJSON;
                            betconfig.CreatedDate = dateCreated;
                            betconfig.TaxConfig = taxJSON;
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
                                CreatedDate = dateCreated,
                                TaxConfig = taxJSON
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

        public async Task<BaseResponseDTO<StatusCode>> ProcessParlayBonus(ParlayBonusRequestDTO payload)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var dateCreated = DateTime.UtcNow;

                    foreach (var parlay in payload.ParlayBonus)
                    {
                        bool isAudit = false;
                        var result = _bettingContext.ParlayBonus.FirstOrDefault(b => b.ClientIdRef == payload.ClientIdRef 
                                            && b.NoOfSelection == parlay.NoOfSelection);
                        if (result != null)
                        {
                            isAudit = result.Bonus != parlay.Bonus;

                            result.Bonus = parlay.Bonus;
                            result.MinOdds = payload.MinOdds;
                            result.ClientId = payload.ClientId;
                            _bettingContext.ParlayBonus.Update(result);
                        }
                        else
                        {
                            _bettingContext.ParlayBonus.Add(new ParlayBonus
                            {
                                ClientId = payload.ClientId,
                                ClientIdRef = parlay.ClientIdRef,
                                NoOfSelection = parlay.NoOfSelection,
                                Bonus = parlay.Bonus,
                                MinOdds = payload.MinOdds
                            });
                            isAudit = true;
                        }

                        if(isAudit)
                        {
                            _bettingContext.ParlayBonusAudit.Add(new ParlayBonusAudit
                            {
                                ClientId = payload.ClientId,
                                ClientIdRef = parlay.ClientIdRef,
                                NoOfSelection = parlay.NoOfSelection,
                                Bonus = parlay.Bonus,
                                TimeStamp = dateCreated,
                                MinOdds = payload.MinOdds
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
        
        public async Task<BaseResponseDTO<ParlayBonusResponseDTO>> GetClientParlayBonus(GetParlayBonusRequestDTO request)
        {
            var result = new ParlayBonusResponseDTO();
            if(request.NoOfSelection == 0)
            {
                if(request.ClientIdRef == 0)
                {
                    var parlay = _bettingContext.ParlayBonus.Where(x => x.ClientId == request.ClientId).ToList();
                    result.ParlayBonus = parlay;
                    if (parlay.Any())
                        result.MinOdds = parlay.FirstOrDefault().MinOdds;
                }
                else
                {
                    var parlay = _bettingContext.ParlayBonus.Where(x => x.ClientIdRef == request.ClientIdRef).ToList();
                    result.ParlayBonus = parlay;
                    if (parlay.Any())
                        result.MinOdds = parlay.FirstOrDefault().MinOdds;
                }
            }
            else
            {
                if(request.ClientIdRef == 0)
                {
                    var parlay = _bettingContext.ParlayBonus.Where(x => x.ClientId == request.ClientId && x.NoOfSelection == request.NoOfSelection).ToList();
                    result.ParlayBonus = parlay;
                    if (parlay.Any())
                        result.MinOdds = parlay.FirstOrDefault().MinOdds;
                }
                else
                {
                    var parlay = _bettingContext.ParlayBonus.Where(x => x.ClientIdRef == request.ClientIdRef && x.NoOfSelection == request.NoOfSelection).ToList();
                    result.ParlayBonus = parlay;
                    if (parlay.Any())
                        result.MinOdds = parlay.FirstOrDefault().MinOdds;
                }
            }

            return new BaseResponseDTO<ParlayBonusResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public async Task<BaseResponseDTO<ParlayBonusAuditResponseDTO>> GetClientParlayBonusHistory(GetParlayBonusRequestDTO request)
        {
            var result = new ParlayBonusAuditResponseDTO();
            if (request.NoOfSelection == 0)
            {
                var parlay = _bettingContext.ParlayBonusAudit.Where(x => x.ClientIdRef == request.ClientIdRef).ToList();
                result.ParlayBonusAudit = parlay;
            }
            else
            {
                var parlay = _bettingContext.ParlayBonusAudit.Where(x => x.ClientIdRef == request.ClientIdRef && x.NoOfSelection == request.NoOfSelection).ToList();
                result.ParlayBonusAudit = parlay;
            }

            return new BaseResponseDTO<ParlayBonusAuditResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }
    }
}
 

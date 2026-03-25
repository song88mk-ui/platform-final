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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Platform.Betting.API.Repository
{
    public class BetConfigRepository : IBetConfigRepository
    {
        private readonly BettingContext _bettingContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BetConfigRepository> _logger;

        public BetConfigRepository(BettingContext bettingContext, IConfiguration configuration,
            ILogger<BetConfigRepository> logger)
        {
            _bettingContext = bettingContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BaseResponseDTO<StatusCode>> ProcessBetConfig(BackOfficeBetConfigPayloadDTO payload)
        {
            try
            {
                var clientID = payload.ClientId;
                var dateCreated = DateTime.UtcNow;

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 50
                };

                Parallel.ForEach(payload.BetConfigPayload, options, config =>
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
                                SportId = 10,
                                SportName = "Soccer",
                                MinStake = config.Football.MinBet,
                                MaxStake = config.Football.MaxBet,
                                StakeLimitFixture = config.Football.MaxFixtureBet
                            },
                            new OverrideSportConfig
                            {
                                SportId = 6,
                                SportName = "Cricket",
                                MinStake = config.Cricket.MinBet,
                                MaxStake = config.Cricket.MaxBet,
                                StakeLimitFixture = config.Cricket.MaxFixtureBet
                            }
                    });

                    var taxJSON = JsonSerializer.Serialize(config.TaxConfig);
                    var priceboostJson = JsonSerializer.Serialize(config.Priceboost);
                    var betBuilderJson = JsonSerializer.Serialize(config.BetBuilder);
                    var parleyJSON = JsonSerializer.Serialize(new List<ParlayBonusConfig>());


                    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                        using (var context = new BettingContext(optionsBuilder.Options))
                        {
                            var betconfig = context.BetConfig.FirstOrDefault(b => b.ClientId == clientID && b.CurrencyCode == config.CurrencyCode);
                            if (betconfig != null)
                            {
                                //retain existing ParlayBonus config
                                var parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betconfig.ParlayConfig);
                                if (parlayConfig != null)
                                {
                                    parleyJSON = JsonSerializer.Serialize(new ParlayConfig
                                    {
                                        MinStake = config.Parlay.MinBet,
                                        MaxStake = config.Parlay.MaxBet,
                                        MaxPayout = config.Parlay.MaxPayout,
                                        ParlayBonusMaxWinnings = config.Parlay.ParlayBonusMaxWinnings,
                                        ParlayBonusMinOdds = parlayConfig.ParlayBonusMinOdds,
                                        ParlayBonusConfig = parlayConfig.ParlayBonusConfig ?? new List<ParlayBonusConfig>()
                                    });
                                }

                                betconfig.DefaultConfig = defaultJSON;
                                betconfig.OverrideConfig = overrideJSON;
                                betconfig.ParlayConfig = parleyJSON;
                                betconfig.CreatedDate = dateCreated;
                                betconfig.TaxConfig = taxJSON;
                                betconfig.PriceboostConfig = priceboostJson;
                                betconfig.BetBuilderConfig = betBuilderJson;
                                context.BetConfig.Update(betconfig);
                            }
                            else
                            {
                                List<ParlayBonusConfig> defaultParlayConfig = null;

                                if (!(defaultParlayConfig?.Any() ?? false))
                                {
                                    var tempConfig = context.BetConfig.Where(b => b.ClientId == clientID && !string.IsNullOrEmpty(b.ParlayConfig));

                                    foreach (var item in tempConfig)
                                    {
                                        var parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(item.ParlayConfig);

                                        if (parlayConfig?.ParlayBonusConfig?.Any() ?? false)
                                        {
                                            config.Parlay.ParlayBonusMinOdds = parlayConfig.ParlayBonusMinOdds;
                                            defaultParlayConfig = parlayConfig.ParlayBonusConfig;
                                            break;
                                        }
                                    }
                                }

                                parleyJSON = JsonSerializer.Serialize(new ParlayConfig
                                {
                                    MinStake = config.Parlay.MinBet,
                                    MaxStake = config.Parlay.MaxBet,
                                    MaxPayout = config.Parlay.MaxPayout,
                                    ParlayBonusMaxWinnings = config.Parlay.ParlayBonusMaxWinnings,
                                    ParlayBonusMinOdds = config.Parlay.ParlayBonusMinOdds,
                                    ParlayBonusConfig = defaultParlayConfig ?? new List<ParlayBonusConfig>()
                                });

                                context.BetConfig.Add(new BetConfig
                                {
                                    ClientId = clientID,
                                    CurrencyCode = config.CurrencyCode,
                                    DefaultConfig = defaultJSON,
                                    OverrideConfig = overrideJSON,
                                    ParlayConfig = parleyJSON,
                                    CreatedDate = dateCreated,
                                    TaxConfig = taxJSON,
                                    PriceboostConfig = priceboostJson,
                                    BetBuilderConfig = betBuilderJson
                                });
                            }

                            //update paylayBonusAudit
                            var parlayAudit = context.ParlayBonusAudit.AsNoTracking().Where(c => c.ClientId == clientID && c.CurrencyCode == config.CurrencyCode)
                                    .OrderByDescending(o => o.TimeStamp).FirstOrDefault();

                            if (parlayAudit != null)
                            {
                                var parlayBonusAudit = context.ParlayBonusAudit.Where(c => c.ClientId == clientID && c.CurrencyCode == config.CurrencyCode && c.TimeStamp == parlayAudit.TimeStamp);

                                foreach (var parlay in parlayBonusAudit)
                                {
                                    parlay.ParlayBonusMaxWinnings = config.Parlay.ParlayBonusMaxWinnings;
                                    context.ParlayBonusAudit.Update(parlay);
                                }
                            }

                            context.SaveChanges();
                        }

                        scope.Complete();
                    }
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ProcessBetConfig: {ex.Message}-{ex.StackTrace}");

                return new BaseResponseDTO<StatusCode>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }

            return new BaseResponseDTO<StatusCode>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString()
            };
        }
        public async Task<BaseResponseDTO<StatusCode>> ProcessUserBetConfig(UserBetConfigRequestDTO payload)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var clientID = new Guid(payload.ClientId);
                    var dateCreated = DateTime.UtcNow;
                    var userBetConfig = _bettingContext.UserBetConfig.FirstOrDefault(x => x.ClientId == clientID && x.UserId == payload.UserId);

                    if (userBetConfig != null)
                    {
                        _bettingContext.UserBetConfig.Update(new UserBetConfig()
                        {
                            UserId = userBetConfig.UserId,
                            ClientId = clientID,
                            CurrencyCode = payload.CurrencyCode,
                            VerticalConfig = payload.VerticalConfig,
                            CreatedDate = userBetConfig.CreatedDate == DateTime.MinValue ? DateTime.UtcNow : userBetConfig.CreatedDate
                        });
                    }
                    else
                    {
                        _bettingContext.UserBetConfig.Add(new UserBetConfig()
                        {
                            UserId = payload.UserId,
                            ClientId = clientID,
                            CurrencyCode = payload.CurrencyCode,
                            VerticalConfig = payload.VerticalConfig,
                            CreatedDate = payload.CreatedDate == DateTime.MinValue ? DateTime.UtcNow : payload.CreatedDate
                        });
                    }

                    await _bettingContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error ProcessUserBetConfig: {ex.Message}-{ex.StackTrace}");

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

                    var result = _bettingContext.BetConfig.Where(b => b.ClientId == payload.ClientId);
                    var parlayAuditMax = 50;
                    var parlayConfigMax = 0;
                    if (payload.ParlayBonusConfig.Any())
                        parlayConfigMax = payload.ParlayBonusConfig.Select(s => s.SelectionCount).Max();

                    if (result != null && result.Any())
                    {
                        foreach (var config in result)
                        {
                            var parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(config.ParlayConfig);
                            parlayConfig.ParlayBonusMinOdds = payload.ParlayBonusMinOdds;

                            if (parlayConfig.ParlayBonusConfig == null)
                            {
                                parlayConfig.ParlayBonusConfig = new List<ParlayBonusConfig>();
                            }

                            if (payload.ParlayBonusConfig.Any() == false && parlayConfig.ParlayBonusConfig.Any())
                            {
                                parlayConfig.ParlayBonusConfig.ForEach(p =>
                                {
                                    _bettingContext.ParlayBonusAudit.Add(new ParlayBonusAudit
                                    {
                                        ClientId = payload.ClientId,
                                        ClientIdRef = payload.ClientIdRef,
                                        SelectionCount = p.SelectionCount,
                                        ParlayBonus = 0m,
                                        ParlayBonusMinOdds = payload.ParlayBonusMinOdds,
                                        ParlayBonusMaxWinnings = parlayConfig.ParlayBonusMaxWinnings,
                                        CurrencyCode = config.CurrencyCode,
                                        TimeStamp = dateCreated
                                    });
                                });

                                parlayConfig.ParlayBonusConfig.Clear();

                            }
                            else
                            {
                                parlayConfig.ParlayBonusConfig.Clear();
                                payload.ParlayBonusConfig.ForEach(p =>
                                {
                                    parlayConfig.ParlayBonusConfig.Add(p);

                                    _bettingContext.ParlayBonusAudit.Add(new ParlayBonusAudit
                                    {
                                        ClientId = payload.ClientId,
                                        ClientIdRef = payload.ClientIdRef,
                                        SelectionCount = p.SelectionCount,
                                        ParlayBonus = p.ParlayBonus,
                                        ParlayBonusMinOdds = payload.ParlayBonusMinOdds,
                                        ParlayBonusMaxWinnings = parlayConfig.ParlayBonusMaxWinnings,
                                        CurrencyCode = config.CurrencyCode,
                                        TimeStamp = dateCreated
                                    });
                                });
                            }

                            //update parlaybonusaudit
                            if (parlayConfigMax >= 3 && parlayConfigMax < parlayAuditMax)
                            {
                                for (int i = (parlayConfigMax + 1); i <= parlayAuditMax; i++)
                                {
                                    _bettingContext.ParlayBonusAudit.Add(new ParlayBonusAudit
                                    {
                                        ClientId = payload.ClientId,
                                        ClientIdRef = payload.ClientIdRef,
                                        SelectionCount = i,
                                        ParlayBonus = 0m,
                                        ParlayBonusMinOdds = payload.ParlayBonusMinOdds,
                                        ParlayBonusMaxWinnings = parlayConfig.ParlayBonusMaxWinnings,
                                        CurrencyCode = config.CurrencyCode,
                                        TimeStamp = dateCreated
                                    });
                                }
                            }

                            config.ParlayConfig = JsonSerializer.Serialize(parlayConfig);

                            _bettingContext.BetConfig.Update(config);
                        }



                    }
                    else
                    {
                        scope.Dispose();
                        return new BaseResponseDTO<StatusCode>
                        {
                            StatusCode = StatusCode.Parlay_Bonus_Not_Updated,
                            Message = StatusCode.Parlay_Bonus_Not_Updated.ToString().Replace('_', ' ')
                        };
                    }

                    await _bettingContext.SaveChangesAsync();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error ProcessParlayBonus: {ex.Message}-{ex.StackTrace}");

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

            if (request.SelectionCount == 0)
            {
                var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == request.ClientId).FirstOrDefault();
                if (betConfig != null)
                {
                    var parlayBonusAudit = _bettingContext.ParlayBonusAudit.AsNoTracking().Where(x => x.ClientId == request.ClientId && x.CurrencyCode == betConfig.CurrencyCode).OrderBy(o => o.SelectionCount).ThenByDescending(o => o.TimeStamp)
                        .Select(s => new
                        {
                            SelectionCount = s.SelectionCount,
                            ParlayBonus = s.ParlayBonus,
                            LastUpdate = s.TimeStamp.Date == DateTime.UtcNow.Date ? "Today" : s.TimeStamp.ToString("dd/MM/yy")
                        }).ToList().Distinct();

                    var parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig);
                    result.ParlayBonusConfig = parlayConfig.ParlayBonusConfig.Select(s => new ParlayBonusConfigResponse
                    {
                        ParlayBonus = s.ParlayBonus,
                        SelectionCount = s.SelectionCount,
                        ParlayBonusHistory = parlayBonusAudit.Where(h => h.SelectionCount == s.SelectionCount).ToList().Count <= 1 ? new List<ParlayBonusHistory>() :
                                parlayBonusAudit.Where(h => h.SelectionCount == s.SelectionCount)
                                    .Select(s => new ParlayBonusHistory
                                    {
                                        ParlayBonus = s.ParlayBonus,
                                        LastUpdate = s.LastUpdate
                                    }).ToList()
                    }).OrderBy(o => o.SelectionCount).ToList();
                    result.ParlayBonusMinOdds = parlayConfig.ParlayBonusMinOdds;
                }
            }
            else
            {
                var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == request.ClientId).FirstOrDefault();
                if (betConfig != null)
                {
                    var parlayBonusAudit = _bettingContext.ParlayBonusAudit.AsNoTracking().Where(x => x.ClientId == request.ClientId && x.CurrencyCode == betConfig.CurrencyCode && x.SelectionCount == request.SelectionCount).OrderByDescending(o => o.TimeStamp)
                        .Select(s => new
                        {
                            SelectionCount = s.SelectionCount,
                            ParlayBonus = s.ParlayBonus,
                            LastUpdate = s.TimeStamp.Date == DateTime.UtcNow.Date ? "Today" : s.TimeStamp.ToString("dd/MM/yy")
                        }).ToList().Distinct();

                    var parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig);
                    result.ParlayBonusConfig = parlayConfig.ParlayBonusConfig.Where(p => p.SelectionCount == request.SelectionCount).Select(s => new ParlayBonusConfigResponse
                    {
                        ParlayBonus = s.ParlayBonus,
                        SelectionCount = s.SelectionCount,
                        ParlayBonusHistory = parlayBonusAudit.Where(h => h.SelectionCount == s.SelectionCount).ToList().Count <= 1 ? new List<ParlayBonusHistory>() :
                                parlayBonusAudit.Where(h => h.SelectionCount == s.SelectionCount)
                                    .Select(bh => new ParlayBonusHistory
                                    {
                                        ParlayBonus = bh.ParlayBonus,
                                        LastUpdate = bh.LastUpdate
                                    }).ToList()
                    }).OrderBy(o => o.SelectionCount).ToList();
                    result.ParlayBonusMinOdds = parlayConfig.ParlayBonusMinOdds;
                }
            }

            return new BaseResponseDTO<ParlayBonusResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

    }
}

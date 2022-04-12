using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace Platform.Betting.API.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly BettingContext _bettingContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private readonly IMTSAuthRepository _mtsAuthRepository;
        private readonly IMTSMaxStakeRepository _mtsMaxStakeRepository;
        private readonly IMTSProbabilityRepository _mtsProbabilityRepository;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IRabbitMQRepository _rabbitMQRepository;

        private readonly TelemetryClient _telemetryClient;

        public BettingRepository(BettingContext bettingContext, IConfiguration configuration, IHttpClientFactory httpClientFactory, IMTSAuthRepository mtsAuthRepository, IMTSMaxStakeRepository mtsMaxStakeRepository, IMTSProbabilityRepository mtsProbabilityRepository, ISportsbookRepository sportsbookRepository, IRabbitMQRepository rabbitMQRepository, TelemetryClient telemetryClient)
        {
            _bettingContext = bettingContext;
            _httpClientFactory = httpClientFactory;
            _mtsAuthRepository = mtsAuthRepository;
            _mtsMaxStakeRepository = mtsMaxStakeRepository;
            _mtsProbabilityRepository = mtsProbabilityRepository;
            _sportsbookRepository = sportsbookRepository;
            _rabbitMQRepository = rabbitMQRepository;
            _telemetryClient = telemetryClient;
            _configuration = configuration;
        }

        public async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request)
        {
            if (request.Selections.Any(c => c.SportId > 1000))
                return await PlaceBetWithoutMTS(request);
            else
            {
                if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) == true)
                    return await PlaceBetWithMTS(request);
                else
                    return await PlaceBetWithoutMTS(request);
            }
        }
        public async Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStake(BetRequestDTO request)
        {
            if (request.IsBoosted == true || request.Selections.Any(c => c.SportId > 1000))
                return await GetMaxStakeWithoutMTS(request);
            else
            {
                if (Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) == true)
                    return await GetMaxStakeWithMTS(request);
                else
                    return await GetMaxStakeWithoutMTS(request);
            }
        }
        private async Task<BaseResponseDTO<BetResponseDTO>> PlaceBetWithoutMTS(BetRequestDTO request)
        {
            if (request.LimitId == "0" || string.IsNullOrEmpty(request.LimitId) || string.IsNullOrWhiteSpace(request.LimitId))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()} - Missing LimitId",
                };
            }

            if(request.IsBoosted == true)
            {
                var boostedSelection = request.Selections.FirstOrDefault();
                var hasExistingBetPriceBoost = _bettingContext.BetPriceBoost.AsNoTracking().Any(b => b.UserId == request.UserId 
                                                && b.FixtureId == boostedSelection.FixtureId 
                                                && b.MarketId == boostedSelection.MarketId
                                                && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled);
                
                if(hasExistingBetPriceBoost == true)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Placing_Multiple_Bets_On_The_Same_Boosted_Odds_Is_Not_Allowed,
                        Message = $"{StatusCode.Placing_Multiple_Bets_On_The_Same_Boosted_Odds_Is_Not_Allowed}".Replace('_', ' '),
                    };
                }
            }

            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == Guid.Parse(request.ClientId) && x.CurrencyCode == request.CurrencyCode.ToString()).FirstOrDefault();

            decimal minStake = 0;
            decimal maxStake = 0;
            decimal maxPerGame = 0;
            decimal stakeFixtureLimit = 0;
            decimal parlayBonus = 0;
            var odds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
            var taxConfig = !string.IsNullOrEmpty(betConfig.TaxConfig) ? JsonSerializer.Deserialize<TaxConfig>(betConfig.TaxConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new TaxConfig();
            var priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();

            if (request.Selections.Count == 1)
            {
                var tierMaxStake = await GetTierMaxStake(new BetConfigLeagueRequestDTO
                {
                    LeagueId = request.Selections.FirstOrDefault().LeagueId,
                    SegmentId = request.Selections.FirstOrDefault().SegmentId,
                    SportId = request.Selections.FirstOrDefault().SportId
                });

                var selection = request.Selections.FirstOrDefault();

                List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                var checkOverride = overrideSportConfig.Where(a => a.SportId == selection.SportId).FirstOrDefault();
                if (checkOverride != null)
                {
                    minStake = GetMinStake(checkOverride.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForSingleBetWithoutMTS(checkOverride.MaxStake, tierMaxStake.MaxStake, request.ExchangeRate, odds);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = checkOverride.StakeLimitFixture < maxPerGame ? checkOverride.StakeLimitFixture : maxPerGame;
                }
                else
                {
                    DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    minStake = GetMinStake(defaultSportConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForSingleBetWithoutMTS(defaultSportConfig.MaxStake, tierMaxStake.MaxStake, request.ExchangeRate, odds);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = defaultSportConfig.StakeLimitFixture < maxPerGame ? defaultSportConfig.StakeLimitFixture  : maxPerGame;
                }

                if(request.IsBoosted == true)
                {
                    maxStake = priceboostConfig.MaxStake;
                }

                if (minStake > request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake.ToString().Replace('_', ' ')
                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var stakeFixtureLimitResult = _bettingContext.StakeLimitFixture.AsNoTracking().Where(a => a.UserId == $"{request.ClientCode}-{request.UserId}" && a.FixtureId == request.Selections.FirstOrDefault().FixtureId).FirstOrDefault();

                if (stakeFixtureLimitResult != null)
                {
                    var remainingFixtureBet = stakeFixtureLimitResult.Remaining - request.Stake;

                    if (remainingFixtureBet < 0)
                    {
                        return new BaseResponseDTO<BetResponseDTO>
                        {
                            StatusCode = StatusCode.Maximum_Bet_Limit_On_Fixture_Reached,
                            Message = StatusCode.Maximum_Bet_Limit_On_Fixture_Reached.ToString().Replace('_', ' ')
                        };
                    }
                }
            }
            else
            {
                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                minStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                maxStake = GetMaxStakeForParlayBetWithoutMTS(parlayConfig.MaxStake, parlayConfig.MaxPayout, odds);

                if (minStake > request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var payout = odds * request.Stake;

                if (parlayConfig.MaxPayout < payout)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Maximum_Payout_Must_Be_Equal_Or_Lower,
                        Message = StatusCode.Maximum_Payout_Must_Be_Equal_Or_Lower.ToString().Replace('_', ' ')
                    };
                }

                var hasVirtualSport = request.Selections.Any(s => s.SportId > 1000) ? true : false;

                if (hasVirtualSport == false && request.Selections.Count >= 3 && parlayConfig.ParlayBonusConfig != null)
                {
                    var selectionCount = request.Selections.Where(c => c.Odds >= parlayConfig.ParlayBonusMinOdds).Count();
                    if (selectionCount >= 3)
                    {
                        var parlayBonusConfig = parlayConfig.ParlayBonusConfig.FirstOrDefault(c => c.SelectionCount == selectionCount);

                        if (parlayBonusConfig != null)
                            parlayBonus = parlayBonusConfig.ParlayBonus;
                    }
                }
            }

            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    Guid betId;

                    var totalOdds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
                    var selectionSports = request.Selections.Select(s => s.SportId).Distinct().ToList();

                    var stakeTax = taxConfig.StakeTax;

                    if (stakeTax > 0)
                    {
                        var excludedTaxedSport = !string.IsNullOrEmpty(taxConfig.ExcludeStakeSportTypes) ? taxConfig.ExcludeStakeSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedTaxedSport.Count == selectionSports.Count)
                            stakeTax = 0m;
                    }

                    var taxedStake = request.Stake - (request.Stake / (1 + stakeTax));
                    var stake = request.Stake - taxedStake;

                    var winningsTax = taxConfig.WinningsTax;

                    if (winningsTax > 0)
                    {
                        var excludedWinningSport = !string.IsNullOrEmpty(taxConfig.ExcludeWinningsSportTypes) ? taxConfig.ExcludeWinningsSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedWinningSport.Count == selectionSports.Count)
                            winningsTax = 0m;
                    }

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = request.Stake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = stake * totalOdds,
                        WinLoss = 0m,
                        BetStatusId = (int)BetStatus.Waiting,
                        BetStatusName = BetStatus.Waiting.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExchangeRate,
                        IPAddress = request.IPAddress,
                        Channel = request.Channel.ToString(),
                        ClientId = request.ClientId,
                        ClientIdRef = request.ClientIdRef,
                        ClientCode = request.ClientCode,
                        ClientName = request.ClientName,
                        LimitId = request.LimitId,
                        Endpoint = request.Endpoint,
                        OperatorToken = request.OperatorToken,
                        CompanyShare = request.CompanyShare,
                        IsResettled = false,
                        IsCashout = false,
                        BetDate = request.BetDate,
                        BetDateOffset = request.BetDate.AddHours(-4),
                        SettlementDate = request.BetDate,
                        SettlementDateOffset = request.BetDate.AddHours(-4),
                        StakeTax = stakeTax,
                        WinningsTax = winningsTax,
                        TaxedStake = 0, //taxedStake,
                        TaxedWinnings = 0,
                        ParlayBonus = parlayBonus,
                        ParlayBonusWinnings = 0,
                        StakeReal = 0,
                        StakeBonus = 0,
                        StakeRealPayout = 0,
                        StakeBonusPayout = 0,
                        IsFreeBet = false
                    }).Entity;

                    betData = betEntity;
                    betId = betEntity.BetId;
                    request.BetId = betId;

                    _bettingContext.BetAudit.Add(new BetAudit
                    {
                        BetId = betEntity.BetId,
                        UserId = betEntity.UserId,
                        Stake = betEntity.Stake,
                        TrueStake = betEntity.TrueStake,
                        Odds = betEntity.Odds,
                        OddsTypeId = betEntity.OddsTypeId,
                        OddsTypeName = betEntity.OddsTypeName,
                        Payout = betEntity.Payout,
                        WinLoss = betEntity.WinLoss,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        Reason = betEntity.Reason,
                        CurrencyCode = betEntity.CurrencyCode,
                        LanguageCode = betEntity.LanguageCode,
                        ExchangeRate = betEntity.ExchangeRate,
                        IPAddress = betEntity.IPAddress,
                        Channel = betEntity.Channel,
                        ClientId = betEntity.ClientId,
                        ClientIdRef = betEntity.ClientIdRef,
                        ClientCode = betEntity.ClientCode,
                        ClientName = betEntity.ClientName,
                        LimitId = betEntity.LimitId,
                        Endpoint = betEntity.Endpoint,
                        OperatorToken = betEntity.OperatorToken,
                        CompanyShare = betEntity.CompanyShare,
                        IsResettled = betEntity.IsResettled,
                        IsCashout = betEntity.IsCashout,
                        BetDate = betEntity.BetDate,
                        BetDateOffset = betEntity.BetDateOffset,
                        SettlementDate = betEntity.SettlementDate,
                        SettlementDateOffset = betEntity.SettlementDateOffset,
                        StakeTax = betEntity.StakeTax,
                        WinningsTax = betEntity.WinningsTax,
                        TaxedStake = betEntity.TaxedStake,
                        TaxedWinnings = betEntity.TaxedWinnings,
                        ParlayBonus = betEntity.ParlayBonus,
                        ParlayBonusWinnings = betEntity.ParlayBonusWinnings,
                        StakeReal = betEntity.StakeReal,
                        StakeBonus = betEntity.StakeBonus,
                        StakeRealPayout = betEntity.StakeRealPayout,
                        StakeBonusPayout = betEntity.StakeBonusPayout,
                        IsFreeBet = betEntity.IsFreeBet
                    });

                    foreach (var selection in request.Selections)
                    {
                        var betSelectionEntity = _bettingContext.BetSelection.Update(new BetSelection
                        {
                            BetId = betId,
                            UserId = request.UserId,
                            SportId = selection.SportId,
                            SportName = selection.SportName,
                            SegmentId = selection.SegmentId,
                            SegmentName = selection.SegmentName,
                            LeagueId = selection.LeagueId,
                            LeagueName = selection.LeagueName,
                            FixtureId = selection.FixtureId,
                            FixtureName = selection.FixtureName,
                            FixtureTypeId = (int)selection.FixtureType,
                            FixtureTypeName = selection.FixtureType.ToString(),
                            MarketTemplateId = selection.MarketTemplateId,
                            MarketId = selection.MarketId,
                            MarketName = selection.MarketName,
                            SelectionId = selection.SelectionId,
                            SelectionName = selection.SelectionName,
                            FixtureStatusId = (int)selection.FixtureStatus,
                            FixtureStatusName = selection.FixtureStatus.ToString(),
                            Line = selection.Line,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            Odds = selection.Odds,
                            BetSelectionStatusId = betEntity.BetStatusId,
                            BetSelectionStatusName = betEntity.BetStatusName,
                            IsSettled = false,
                            IsResettled = false,
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            FixtureStartDate = selection.FixtureStartDate,
                            BetScore = selection.BetScore,
                            SettlementScore = selection.BetScore,
                            Probability = selection.Probability,
                            WonProbability = selection.WonProbability,
                            LostProbability = selection.LostProbability,
                            VoidProbability = selection.VoidProbability,
                            HalfWonProbability = selection.HalfWonProbability,
                            HalfLostProbability = selection.HalfLostProbability,
                            CorrelationId = selection.CorrelationId,
                            DeadHeatFactor = 1m
                        }).Entity;

                        betSelectionData.Add(betSelectionEntity);

                        var betSelectionId = betSelectionEntity.BetSelectionId;
                        selection.BetSelectionId = betSelectionId;

                        _bettingContext.BetSelectionAudit.Add(new BetSelectionAudit
                        {
                            BetId = betSelectionEntity.BetId,
                            BetSelectionId = betSelectionEntity.BetSelectionId,
                            UserId = betSelectionEntity.UserId,
                            SportId = betSelectionEntity.SportId,
                            SportName = betSelectionEntity.SportName,
                            SegmentId = betSelectionEntity.SegmentId,
                            SegmentName = betSelectionEntity.SegmentName,
                            LeagueId = betSelectionEntity.LeagueId,
                            LeagueName = betSelectionEntity.LeagueName,
                            FixtureId = betSelectionEntity.FixtureId,
                            FixtureName = betSelectionEntity.FixtureName,
                            FixtureTypeId = betSelectionEntity.FixtureTypeId,
                            FixtureTypeName = betSelectionEntity.FixtureTypeName,
                            MarketTemplateId = betSelectionEntity.MarketTemplateId,
                            MarketId = betSelectionEntity.MarketId,
                            MarketName = betSelectionEntity.MarketName,
                            SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                            SelectionId = betSelectionEntity.SelectionId,
                            SelectionName = betSelectionEntity.SelectionName,
                            FixtureStatusId = betSelectionEntity.FixtureStatusId,
                            FixtureStatusName = betSelectionEntity.FixtureStatusName,
                            Line = betSelectionEntity.Line,
                            Odds = betSelectionEntity.Odds,
                            BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                            BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                            IsSettled = betSelectionEntity.IsSettled,
                            IsResettled = betSelectionEntity.IsResettled,
                            FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                            SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                            FixtureStartDate = betSelectionEntity.FixtureStartDate,
                            BetScore = betSelectionEntity.BetScore,
                            SettlementScore = betSelectionEntity.SettlementScore,
                            Probability = betSelectionEntity.Probability,
                            WonProbability = betSelectionEntity.WonProbability,
                            LostProbability = betSelectionEntity.LostProbability,
                            VoidProbability = betSelectionEntity.VoidProbability,
                            HalfWonProbability = betSelectionEntity.HalfWonProbability,
                            HalfLostProbability = betSelectionEntity.HalfLostProbability,
                            CorrelationId = selection.CorrelationId,
                            DeadHeatFactor = betSelectionEntity.DeadHeatFactor
                        });

                        if (request.IsBoosted == true)
                        {
                            _bettingContext.BetPriceBoost.Add(new BetPriceBoost()
                            {
                                BetId = betSelectionEntity.BetId,
                                UserId = betSelectionEntity.UserId,
                                FixtureId = betSelectionEntity.FixtureId,
                                MarketId = betSelectionEntity.MarketId,
                                BetStatusId = betEntity.BetStatusId,
                                BetStatusName = betEntity.BetStatusName,
                            });
                        }
                    }

                    if (request.Selections.Count == 1)
                    {
                        var stakeLimit = _bettingContext.StakeLimitFixture.Find($"{request.ClientCode}-{request.UserId}", request.Selections.First().FixtureId);

                        if (stakeLimit == null)
                        {
                            _bettingContext.StakeLimitFixture.Add(new StakeLimitFixture
                            {
                                UserId = $"{request.ClientCode}-{request.UserId}",
                                FixtureId = request.Selections.First().FixtureId,
                                Remaining = stakeFixtureLimit - request.Stake
                            });
                        }
                        else
                        {
                            stakeLimit.Remaining -= request.Stake;
                            _bettingContext.StakeLimitFixture.Update(stakeLimit);
                        }
                    }

                    _bettingContext.SaveChanges();
                    scope.Complete();

                    //_messageBroker.Publish(new OnBetPlacement
                    //{
                    //    Bet = betData,
                    //    BetSelections = betSelectionData
                    //}, "OnBetPlacement", "r.bet.placement", Guid.NewGuid().ToString().ToUpper());

                    var payload = new OnBetPlacement
                    {
                        Bet = betData,
                        BetSelections = betSelectionData,
                        IsBoosted = request.IsBoosted,
                    };

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(payload),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnBetPlacement",
                        RoutingKey = "r.bet.placement"
                    });
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>
                            {
                                { "BetId", betData.BetId.ToString().ToUpper() },
                                { "Error", ex.Message }
                            };

                _telemetryClient.TrackEvent("BetWithoutMTS", properties);

                //_messageBroker.Publish(new OnBetPlacementError
                //{
                //    BetId = betData.BetId.ToString(),
                //    Error = ex.Message,
                //    StackTrace = ex.StackTrace,
                //    InnerException = ex.InnerException.ToString()
                //}, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                var payload = new OnBetPlacementError
                {
                    BetId = betData.BetId.ToString(),
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException.ToString()
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnBetPlacementError",
                    RoutingKey = "r.bet.placement.error"
                });

                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

            return new BaseResponseDTO<BetResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetResponseDTO
                {
                    UserId = request.UserId,
                    BetId = request.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Waiting
                }
            };
        }
        private async Task<BaseResponseDTO<BetResponseDTO>> PlaceBetWithMTS(BetRequestDTO request)
        {
            if (request.LimitId == "0" || string.IsNullOrEmpty(request.LimitId) || string.IsNullOrWhiteSpace(request.LimitId))
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()} - Missing LimitId",
                };
            }

            if (request.IsBoosted == true)
            {
                var boostedSelection = request.Selections.FirstOrDefault();
                var hasExistingBetPriceBoost = _bettingContext.BetPriceBoost.AsNoTracking().Any(b => b.UserId == request.UserId 
                                                && b.FixtureId == boostedSelection.FixtureId 
                                                && b.MarketId == boostedSelection.MarketId
                                                && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled);

                if (hasExistingBetPriceBoost == true)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Placing_Multiple_Bets_On_The_Same_Boosted_Odds_Is_Not_Allowed,
                        Message = $"{StatusCode.Placing_Multiple_Bets_On_The_Same_Boosted_Odds_Is_Not_Allowed}".Replace('_', ' '),
                    };
                }
            }

            var mtsConfigSelectionRequest = new List<MTSConfigSelectionRequestDTO>();
            foreach (var betSelection in request.Selections)
            {
                mtsConfigSelectionRequest.Add(new MTSConfigSelectionRequestDTO
                {
                    FixtureSpecifier = betSelection.FixtureSpecifier,
                    SelectionSpecifier = betSelection.SelectionSpecifier,
                    Odds = betSelection.Odds
                });
            }

            var mtsConfigRequest = new MTSConfigRequestDTO
            {
                UserId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                Currency = request.CurrencyCode.ToString(),
                Language = request.LanguageCode.ToString(),
                DeviceId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                LimitId = Convert.ToInt32(request.LimitId),
                BetSelections = mtsConfigSelectionRequest
            };

            var mtsAuthToken = await _mtsAuthRepository.GetMTSAuthorizationToken();

            if (mtsAuthToken.StatusCode != StatusCode.Success)
                return await PlaceBetWithoutMTS(request);

            var mtsMaxStake = await _mtsMaxStakeRepository.GetMTSMaxStake(mtsAuthToken.Result.Token, mtsConfigRequest, request.CompanyShare);

            if (mtsMaxStake.StatusCode != StatusCode.Success)
                return await PlaceBetWithoutMTS(request);

            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == Guid.Parse(request.ClientId) && x.CurrencyCode == request.CurrencyCode.ToString()).FirstOrDefault();

            decimal minStake = 0;
            decimal maxStake = 0;
            decimal maxPerGame = 0;
            decimal stakeFixtureLimit = 0;
            decimal parlayBonus = 0;
            var odds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
            var taxConfig = !string.IsNullOrEmpty(betConfig.TaxConfig) ? JsonSerializer.Deserialize<TaxConfig>(betConfig.TaxConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new TaxConfig();
            var priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();

            if (request.Selections.Count == 1)
            {
                var tierMaxStake = await GetTierMaxStake(new BetConfigLeagueRequestDTO
                {
                    LeagueId = request.Selections.FirstOrDefault().LeagueId,
                    SegmentId = request.Selections.FirstOrDefault().SegmentId,
                    SportId = request.Selections.FirstOrDefault().SportId
                });

                var selection = request.Selections.FirstOrDefault();

                List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                var checkOverride = overrideSportConfig.Where(a => a.SportId == selection.SportId).FirstOrDefault();
                if (checkOverride != null)
                {
                    minStake = GetMinStake(checkOverride.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForSingleBetWithMTS(checkOverride.MaxStake, tierMaxStake.MaxStake, mtsMaxStake.Result.Stake, request.ExchangeRate, odds, request.CompanyShare);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = checkOverride.StakeLimitFixture < maxPerGame ? checkOverride.StakeLimitFixture : maxPerGame;
                }
                else
                {
                    DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    minStake = GetMinStake(defaultSportConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForSingleBetWithMTS(defaultSportConfig.MaxStake, tierMaxStake.MaxStake, mtsMaxStake.Result.Stake, request.ExchangeRate, odds, request.CompanyShare);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = defaultSportConfig.StakeLimitFixture < maxPerGame ? defaultSportConfig.StakeLimitFixture : maxPerGame;
                }

                if (request.IsBoosted == true)
                {
                    maxStake = priceboostConfig.MaxStake;
                }

                if (minStake > request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var stakeFixtureLimitResult = _bettingContext.StakeLimitFixture.AsNoTracking().Where(a => a.UserId == $"{request.ClientCode}-{request.UserId}" && a.FixtureId == request.Selections.FirstOrDefault().FixtureId).FirstOrDefault();

                if (stakeFixtureLimitResult == null)
                {
                    if (stakeFixtureLimit > maxStake)
                        stakeFixtureLimit = maxStake;
                }
            }
            else
            {

                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                minStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                maxStake = GetMaxStakeForParlayBetWithMTS(parlayConfig.MaxStake, mtsMaxStake.Result.Stake, parlayConfig.MaxPayout, odds, request.CompanyShare);

                if (minStake > request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Higher_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Equal_Or_Lower_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var hasVirtualSport = request.Selections.Any(s => s.SportId > 1000) ? true : false;

                if (hasVirtualSport == false && request.Selections.Count >= 3 && parlayConfig.ParlayBonusConfig != null)
                {
                    var selectionCount = request.Selections.Where(c => c.Odds >= parlayConfig.ParlayBonusMinOdds).Count();
                    if (selectionCount >= 3)
                    {
                        var parlayBonusConfig = parlayConfig.ParlayBonusConfig.FirstOrDefault(c => c.SelectionCount == selectionCount);

                        if (parlayBonusConfig != null)
                            parlayBonus = parlayBonusConfig.ParlayBonus;
                    }
                }
            }

            var betData = new Bet();
            var betSelectionData = new List<BetSelection>();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    Guid betId;

                    var totalOdds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
                    var selectionSports = request.Selections.Select(s => s.SportId).Distinct().ToList();

                    var stakeTax = taxConfig.StakeTax;

                    if (stakeTax > 0)
                    {
                        var excludedTaxedSport = !string.IsNullOrEmpty(taxConfig.ExcludeStakeSportTypes) ? taxConfig.ExcludeStakeSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedTaxedSport.Count == selectionSports.Count)
                            stakeTax = 0m;
                    }

                    var taxedStake = 0m; // request.Stake - (request.Stake / (1 + stakeTax));
                    var stake = request.Stake - taxedStake;

                    var winningsTax = taxConfig.WinningsTax;

                    if (winningsTax > 0)
                    {
                        var excludedWinningSport = !string.IsNullOrEmpty(taxConfig.ExcludeWinningsSportTypes) ? taxConfig.ExcludeWinningsSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedWinningSport.Count == selectionSports.Count)
                            winningsTax = 0m;
                    }

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = request.Stake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = stake * totalOdds,
                        WinLoss = 0m,
                        BetStatusId = (int)BetStatus.Waiting,
                        BetStatusName = BetStatus.Waiting.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExchangeRate,
                        IPAddress = request.IPAddress,
                        Channel = request.Channel.ToString(),
                        ClientId = request.ClientId,
                        ClientIdRef = request.ClientIdRef,
                        ClientCode = request.ClientCode,
                        ClientName = request.ClientName,
                        LimitId = request.LimitId,
                        Endpoint = request.Endpoint,
                        OperatorToken = request.OperatorToken,
                        CompanyShare = request.CompanyShare,
                        IsResettled = false,
                        IsCashout = false,
                        BetDate = request.BetDate,
                        BetDateOffset = request.BetDate.AddHours(-4),
                        SettlementDate = request.BetDate,
                        SettlementDateOffset = request.BetDate.AddHours(-4),
                        StakeTax = stakeTax,
                        WinningsTax = winningsTax,
                        TaxedStake = 0, // taxedStake,
                        TaxedWinnings = 0,
                        ParlayBonus = parlayBonus,
                        ParlayBonusWinnings = 0,
                        StakeReal = 0,
                        StakeBonus = 0,
                        StakeRealPayout = 0,
                        StakeBonusPayout = 0,
                        IsFreeBet = false
                    }).Entity;

                    betData = betEntity;
                    betId = betEntity.BetId;
                    request.BetId = betId;

                    _bettingContext.BetAudit.Add(new BetAudit
                    {
                        BetId = betEntity.BetId,
                        UserId = betEntity.UserId,
                        Stake = betEntity.Stake,
                        TrueStake = betEntity.TrueStake,
                        Odds = betEntity.Odds,
                        OddsTypeId = betEntity.OddsTypeId,
                        OddsTypeName = betEntity.OddsTypeName,
                        Payout = betEntity.Payout,
                        WinLoss = betEntity.WinLoss,
                        BetStatusId = betEntity.BetStatusId,
                        BetStatusName = betEntity.BetStatusName,
                        Reason = betEntity.Reason,
                        CurrencyCode = betEntity.CurrencyCode,
                        LanguageCode = betEntity.LanguageCode,
                        ExchangeRate = betEntity.ExchangeRate,
                        IPAddress = betEntity.IPAddress,
                        Channel = betEntity.Channel,
                        ClientId = betEntity.ClientId,
                        ClientIdRef = betEntity.ClientIdRef,
                        ClientCode = betEntity.ClientCode,
                        ClientName = betEntity.ClientName,
                        LimitId = betEntity.LimitId,
                        Endpoint = betEntity.Endpoint,
                        OperatorToken = betEntity.OperatorToken,
                        CompanyShare = betEntity.CompanyShare,
                        IsResettled = betEntity.IsResettled,
                        IsCashout = betEntity.IsCashout,
                        BetDate = betEntity.BetDate,
                        BetDateOffset = betEntity.BetDateOffset,
                        SettlementDate = betEntity.SettlementDate,
                        SettlementDateOffset = betEntity.SettlementDateOffset,
                        StakeTax = betEntity.StakeTax,
                        WinningsTax = betEntity.WinningsTax,
                        TaxedStake = betEntity.TaxedStake,
                        TaxedWinnings = betEntity.TaxedWinnings,
                        ParlayBonus = betEntity.ParlayBonus,
                        ParlayBonusWinnings = betEntity.ParlayBonusWinnings,
                        StakeReal = betEntity.StakeReal,
                        StakeBonus = betEntity.StakeBonus,
                        StakeRealPayout = betEntity.StakeRealPayout,
                        StakeBonusPayout = betEntity.StakeBonusPayout,
                        IsFreeBet = betEntity.IsFreeBet
                    });

                    foreach (var selection in request.Selections)
                    {
                        var betSelectionEntity = _bettingContext.BetSelection.Update(new BetSelection
                        {
                            BetId = betId,
                            UserId = request.UserId,
                            SportId = selection.SportId,
                            SportName = selection.SportName,
                            SegmentId = selection.SegmentId,
                            SegmentName = selection.SegmentName,
                            LeagueId = selection.LeagueId,
                            LeagueName = selection.LeagueName,
                            FixtureId = selection.FixtureId,
                            FixtureName = selection.FixtureName,
                            FixtureTypeId = (int)selection.FixtureType,
                            FixtureTypeName = selection.FixtureType.ToString(),
                            MarketTemplateId = selection.MarketTemplateId,
                            MarketId = selection.MarketId,
                            MarketName = selection.MarketName,
                            SelectionId = selection.SelectionId,
                            SelectionName = selection.SelectionName,
                            FixtureStatusId = (int)selection.FixtureStatus,
                            FixtureStatusName = selection.FixtureStatus.ToString(),
                            Line = selection.Line,
                            SelectionTemplateId = selection.SelectionTemplateId,
                            Odds = selection.Odds,
                            BetSelectionStatusId = betEntity.BetStatusId,
                            BetSelectionStatusName = betEntity.BetStatusName,
                            IsSettled = false,
                            IsResettled = false,
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            FixtureStartDate = selection.FixtureStartDate,
                            BetScore = selection.BetScore,
                            SettlementScore = selection.BetScore,
                            Probability = selection.Probability,
                            WonProbability = selection.WonProbability,
                            LostProbability = selection.LostProbability,
                            VoidProbability = selection.VoidProbability,
                            HalfWonProbability = selection.HalfWonProbability,
                            HalfLostProbability = selection.HalfLostProbability,
                            CorrelationId = selection.CorrelationId,
                            DeadHeatFactor = 1m
                        }).Entity;

                        betSelectionData.Add(betSelectionEntity);

                        var betSelectionId = betSelectionEntity.BetSelectionId;
                        selection.BetSelectionId = betSelectionId;

                        _bettingContext.BetSelectionAudit.Add(new BetSelectionAudit
                        {
                            BetId = betSelectionEntity.BetId,
                            BetSelectionId = betSelectionEntity.BetSelectionId,
                            UserId = betSelectionEntity.UserId,
                            SportId = betSelectionEntity.SportId,
                            SportName = betSelectionEntity.SportName,
                            SegmentId = betSelectionEntity.SegmentId,
                            SegmentName = betSelectionEntity.SegmentName,
                            LeagueId = betSelectionEntity.LeagueId,
                            LeagueName = betSelectionEntity.LeagueName,
                            FixtureId = betSelectionEntity.FixtureId,
                            FixtureName = betSelectionEntity.FixtureName,
                            FixtureTypeId = betSelectionEntity.FixtureTypeId,
                            FixtureTypeName = betSelectionEntity.FixtureTypeName,
                            MarketTemplateId = betSelectionEntity.MarketTemplateId,
                            MarketId = betSelectionEntity.MarketId,
                            MarketName = betSelectionEntity.MarketName,
                            SelectionTemplateId = betSelectionEntity.SelectionTemplateId,
                            SelectionId = betSelectionEntity.SelectionId,
                            SelectionName = betSelectionEntity.SelectionName,
                            FixtureStatusId = betSelectionEntity.FixtureStatusId,
                            FixtureStatusName = betSelectionEntity.FixtureStatusName,
                            Line = betSelectionEntity.Line,
                            Odds = betSelectionEntity.Odds,
                            BetSelectionStatusId = betSelectionEntity.BetSelectionStatusId,
                            BetSelectionStatusName = betSelectionEntity.BetSelectionStatusName,
                            IsSettled = betSelectionEntity.IsSettled,
                            IsResettled = betSelectionEntity.IsResettled,
                            FixtureSpecifier = betSelectionEntity.FixtureSpecifier,
                            SelectionSpecifier = betSelectionEntity.SelectionSpecifier,
                            FixtureStartDate = betSelectionEntity.FixtureStartDate,
                            BetScore = betSelectionEntity.BetScore,
                            SettlementScore = betSelectionEntity.SettlementScore,
                            Probability = betSelectionEntity.Probability,
                            WonProbability = betSelectionEntity.WonProbability,
                            LostProbability = betSelectionEntity.LostProbability,
                            VoidProbability = betSelectionEntity.VoidProbability,
                            HalfWonProbability = betSelectionEntity.HalfWonProbability,
                            HalfLostProbability = betSelectionEntity.HalfLostProbability,
                            CorrelationId = selection.CorrelationId,
                            DeadHeatFactor = betSelectionEntity.DeadHeatFactor
                        });

                        if(request.IsBoosted == true)
                        {
                            _bettingContext.BetPriceBoost.Add(new BetPriceBoost()
                            {
                                BetId = betSelectionEntity.BetId,
                                UserId = betSelectionEntity.UserId,
                                FixtureId = betSelectionEntity.FixtureId,
                                MarketId = betSelectionEntity.MarketId,
                                BetStatusId = betEntity.BetStatusId,
                                BetStatusName = betEntity.BetStatusName,
                            });
                        }

                    }

                    //if (request.Selections.Count == 1)
                    //{
                    //    var stakeLimit = _bettingContext.StakeLimitFixture.Find($"{request.ClientCode}-{request.UserId}", request.Selections.First().FixtureId);

                    //    if (stakeLimit == null)
                    //    {
                    //        _bettingContext.StakeLimitFixture.Add(new StakeLimitFixture
                    //        {
                    //            UserId = $"{request.ClientCode}-{request.UserId}",
                    //            FixtureId = request.Selections.First().FixtureId,
                    //            Remaining = stakeFixtureLimit - request.Stake
                    //        });
                    //    }
                    //    else
                    //    {
                    //        stakeLimit.Remaining -= request.Stake;
                    //        _bettingContext.StakeLimitFixture.Update(stakeLimit);
                    //    }
                    //}

                    _bettingContext.SaveChanges();
                    scope.Complete();

                    //_messageBroker.Publish(new OnBetPlacement
                    //{
                    //    Bet = betData,
                    //    BetSelections = betSelectionData
                    //}, "OnBetPlacement", "r.bet.placement", Guid.NewGuid().ToString().ToUpper());

                    var payload = new OnBetPlacement
                    {
                        Bet = betData,
                        BetSelections = betSelectionData,
                        IsBoosted = request.IsBoosted,
                    };

                    _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                    {
                        Payload = JsonSerializer.Serialize(payload),
                        CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                        MessageType = "OnBetPlacement",
                        RoutingKey = "r.bet.placement"
                    });
                }
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>
                            {
                                { "BetId", betData.BetId.ToString().ToUpper() },
                                { "Error", ex.Message }
                            };

                _telemetryClient.TrackEvent("BetWithMTS", properties);

                //_messageBroker.Publish(new OnBetPlacementError
                //{
                //    BetId = betData.BetId.ToString(),
                //    Error = ex.Message,
                //    StackTrace = ex.StackTrace,
                //    InnerException = ex.InnerException.ToString()
                //}, "OnBetPlacementError", "r.bet.placement.error", Guid.NewGuid().ToString().ToUpper());

                var payload = new OnBetPlacementError
                {
                    BetId = betData.BetId.ToString(),
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException.ToString()
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnBetPlacementError",
                    RoutingKey = "r.bet.placement.error"
                });

                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }

            return new BaseResponseDTO<BetResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetResponseDTO
                {
                    UserId = request.UserId,
                    BetId = request.BetId.ToString().ToUpper(),
                    BetStatus = BetStatus.Waiting
                }
            };
        }
        private async Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStakeWithMTS(BetRequestDTO request)
        {
            var mtsConfigSelectionRequest = new List<MTSConfigSelectionRequestDTO>();
            foreach (var betSelection in request.Selections)
            {
                mtsConfigSelectionRequest.Add(new MTSConfigSelectionRequestDTO
                {
                    FixtureSpecifier = betSelection.FixtureSpecifier,
                    SelectionSpecifier = betSelection.SelectionSpecifier,
                    Odds = betSelection.Odds
                });
            }

            var mtsConfigRequest = new MTSConfigRequestDTO
            {
                UserId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                Currency = request.CurrencyCode.ToString(),
                Language = request.LanguageCode.ToString(),
                DeviceId = $"{request.ClientCode}{request.UserId.Replace("-", "")}",
                LimitId = Convert.ToInt32(request.LimitId),
                BetSelections = mtsConfigSelectionRequest
            };

            var mtsAuthToken = await _mtsAuthRepository.GetMTSAuthorizationToken();

            if (mtsAuthToken.StatusCode != StatusCode.Success)
                return await GetMaxStakeWithoutMTS(request);

            var mtsMaxStake = await _mtsMaxStakeRepository.GetMTSMaxStake(mtsAuthToken.Result.Token, mtsConfigRequest, request.CompanyShare);

            if (mtsMaxStake.StatusCode != StatusCode.Success)
                return await GetMaxStakeWithoutMTS(request);

            return new BaseResponseDTO<MaxStakeResponseDTO>
            {
                StatusCode = mtsMaxStake.StatusCode,
                Message = $"{mtsMaxStake.Message}",
                Result = new MaxStakeResponseDTO
                {
                    Stake = mtsMaxStake.Result.Stake
                }
            };
        }
        private async Task<BaseResponseDTO<MaxStakeResponseDTO>> GetMaxStakeWithoutMTS(BetRequestDTO request)
        {
            if (request.LimitId == "0" || string.IsNullOrEmpty(request.LimitId) || string.IsNullOrWhiteSpace(request.LimitId))
            {
                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString()} - Missing LimitId",
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = 0
                    }
                };
            }

            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == Guid.Parse(request.ClientId) && x.CurrencyCode == request.CurrencyCode.ToString()).FirstOrDefault();

            decimal maxStake = 0;
            decimal maxPerGame = 0;
            decimal stakeFixtureLimit = 0;
            var odds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
            var priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();

            if (request.Selections.Count == 1)
            {
                var tierMaxStake = await GetTierMaxStake(new BetConfigLeagueRequestDTO
                {
                    LeagueId = request.Selections.FirstOrDefault().LeagueId,
                    SegmentId = request.Selections.FirstOrDefault().SegmentId,
                    SportId = request.Selections.FirstOrDefault().SportId
                });

                var selection = request.Selections.FirstOrDefault();

                List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                var checkOverride = overrideSportConfig.Where(a => a.SportId == selection.SportId).FirstOrDefault();
                if (checkOverride != null)
                {
                    maxStake = GetMaxStakeForSingleBetWithoutMTS(checkOverride.MaxStake, tierMaxStake.MaxStake, request.ExchangeRate, odds);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = checkOverride.StakeLimitFixture < maxPerGame ? checkOverride.StakeLimitFixture : maxPerGame;
                }
                else
                {
                    DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    maxStake = GetMaxStakeForSingleBetWithoutMTS(defaultSportConfig.MaxStake, tierMaxStake.MaxStake, request.ExchangeRate, odds);
                    maxPerGame = tierMaxStake.MaxPerGame * request.ExchangeRate;
                    stakeFixtureLimit = defaultSportConfig.StakeLimitFixture < maxPerGame ? defaultSportConfig.StakeLimitFixture : maxPerGame;
                }

                if (request.IsBoosted == true)
                {
                    maxStake = priceboostConfig.MaxStake;
                }

                var stakeFixtureLimitResult = _bettingContext.StakeLimitFixture.AsNoTracking().Where(a => a.UserId == $"{request.ClientCode}-{request.UserId}" && a.FixtureId == request.Selections.FirstOrDefault().FixtureId).FirstOrDefault();

                if (stakeFixtureLimitResult != null)
                {
                    var remainingStake = stakeFixtureLimitResult.Remaining;

                    return new BaseResponseDTO<MaxStakeResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = new MaxStakeResponseDTO
                        {
                            Stake = remainingStake < 0 ? 0 : remainingStake
                        }
                    };
                }
                else
                {
                    if (stakeFixtureLimit < maxStake)
                    {
                        return new BaseResponseDTO<MaxStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new MaxStakeResponseDTO
                            {
                                Stake = stakeFixtureLimit
                            }
                        };
                    }
                    else
                    {
                        return new BaseResponseDTO<MaxStakeResponseDTO>
                        {
                            StatusCode = StatusCode.Success,
                            Message = StatusCode.Success.ToString(),
                            Result = new MaxStakeResponseDTO
                            {
                                Stake = maxStake
                            }
                        };
                    }
                }
            }
            else
            {
                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                maxStake = GetMaxStakeForParlayBetWithoutMTS(parlayConfig.MaxStake, parlayConfig.MaxPayout, odds);

                return new BaseResponseDTO<MaxStakeResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new MaxStakeResponseDTO
                    {
                        Stake = maxStake
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<BetHistoryResponseDTO>> GetBetHistory(BetHistoryRequestDTO request)
        {
            request.Page = request.Page <= 0 ? 1 : request.Page;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            request.PageSize = request.PageSize > 50 ? 50 : request.PageSize;

            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;
            decimal winLoss = 0m;
            var result = new List<BetHistoryDTO>();
            var transactions = new List<Bet>();

            if (request.IsCurrent)
            {
                var waitingBets = new List<int> { (int)BetStatus.Waiting };
                var waitingBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && waitingBets.Contains(c.BetStatusId) && c.BetDate.AddSeconds(15) >= DateTime.UtcNow).ToList();

                var openBets = new List<int> { (int)BetStatus.Running };
                var openBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId)).ToList();

                var cancelledBets = new List<int> { (int)BetStatus.Cancelled};
                var cancelledBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && cancelledBets.Contains(c.BetStatusId) && c.BetDate.AddMinutes(5) >= DateTime.UtcNow).ToList();

                if (waitingBetsResponse.Count > 0)
                    transactions.AddRange(waitingBetsResponse);

                if (openBetsResponse.Count > 0)
                    transactions.AddRange(openBetsResponse);

                if (cancelledBetsResponse.Count > 0)
                    transactions.AddRange(cancelledBetsResponse);

                rowCount = Convert.ToDouble(transactions.Count());
            }
            else
            {
                var openBets = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running };

                if (request.BetStatusFilter == BetStatusFilter.All)
                {
                    var rawAll = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && c.BetStatusId >= (int)BetStatus.Waiting && c.BetStatusId != (int)BetStatus.Rejected && c.BetDate.Date >= request.FromDate && c.BetDate.Date <= request.ToDate.Date);

                    rowCount = rawAll.Count();
                    winLoss = rawAll.Sum(c => c.WinLoss);
                    transactions = rawAll.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Open)
                {
                    var rawOpen = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId) && c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date);

                    rowCount = rawOpen.Count();
                    winLoss = rawOpen.Sum(c => c.WinLoss);
                    transactions = rawOpen.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Settled)
                {
                    var rawSettled = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && c.BetStatusId > (int)BetStatus.Running && c.BetStatusId != (int)BetStatus.Rejected && c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date);

                    rowCount = rawSettled.Count();
                    winLoss = rawSettled.Sum(c => c.WinLoss);
                    transactions = rawSettled.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }
            }

            if (transactions.Count == 0)
            {
                return new BaseResponseDTO<BetHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetHistoryResponseDTO
                    {
                        CurrentPage = 1,
                        PageCount = 1,
                        PageSize = 1,
                        RowCount = 0,
                        TotalCommission = 0,
                        TotalWinLoss = 0,
                        TotalWLC = 0,
                        Bets = new List<BetHistoryDTO>()
                    }
                };
            }

            var betIds = transactions.Select(s => s.BetId).Distinct().ToList();
            var selections = _bettingContext.BetSelection.AsNoTracking().Where(c => betIds.Contains(c.BetId)).ToList();
            
            ////var cashOutTasks = new List<Task<BaseResponseDTO<CashOutResponse>>>();

            ////foreach (var bet in transactions)
            ////{
            ////    var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
            ////    cashOutTasks.Add(_mtsProbabilityRepository.GetCashOut(bet, filteredSelections));
            ////}

            ////await Task.WhenAll(cashOutTasks);

            ////foreach (var bet in transactions)
            ////{
            ////    var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
            ////    var cashOut = cashOutTasks.Where(c => c.Result.Result.BetId == bet.BetId).FirstOrDefault();

            ////    result.Add(new BetHistoryDTO
            ////    {
            ////        BetId = bet.BetId.ToString(),
            ////        Stake = bet.Stake,
            ////        Odds = bet.Odds,
            ////        Payout = bet.Payout,
            ////        WinLoss = bet.WinLoss,
            ////        BetStatusId = bet.BetStatusId,
            ////        BetStatusName = bet.BetStatusName,
            ////        TransactionDate = bet.BetDate,
            ////        Commission = 0,
            ////        CurrencyCode = bet.CurrencyCode,
            ////        OddsType = bet.OddsTypeName,
            ////        CashOutStake = cashOut.Result.Result.CashOutStake,
            ////        CashOutStatusId = cashOut.Result.StatusCode == StatusCode.Success ? (int)CashOutStatus.Available : (int)CashOutStatus.Unavailable,
            ////        CashOutStatusName = cashOut.Result.StatusCode == StatusCode.Success ? CashOutStatus.Available.ToString() : CashOutStatus.Unavailable.ToString(),
            ////        StakeTax = bet.StakeTax,
            ////        WinningsTax = bet.WinningsTax,
            ////        TaxedStake = bet.TaxedStake,
            ////        TaxedWinnings = bet.TaxedWinnings,
            ////        Selections = filteredSelections.Select(s => new BetSelectionHistoryDTO
            ////        {
            ////            SportId = s.SportId,
            ////            SportName = s.SportName,
            ////            LeagueId = s.LeagueId,
            ////            LeagueName = s.LeagueName,
            ////            FixtureId = s.FixtureId.ToString(),
            ////            FixtureName = s.FixtureName,
            ////            MarketId = s.MarketId.ToString(),
            ////            MarketName = s.MarketName,
            ////            Line = s.Line,
            ////            SelectionId = s.SelectionId.ToString(),
            ////            SelectionName = s.SelectionName,
            ////            Odds = s.Odds,
            ////            BetScore = s.BetScore,
            ////            SettlementScore = s.SettlementScore,
            ////            BetSelectionId = s.BetSelectionId.ToString(),
            ////            FixtureStartDate = s.FixtureStartDate,
            ////            FixtureStatusId = s.FixtureStatusId,
            ////            FixtureStatusName = s.FixtureStatusName,
            ////            MarketTemplateId = s.MarketTemplateId,
            ////            SegmentId = s.SegmentId,
            ////            SegmentName = s.SegmentName,
            ////            SelectionTemplateId = s.SelectionTemplateId,
            ////            BetSelectionStatusId = s.BetSelectionStatusId,
            ////            BetSelectionStatusName = s.BetSelectionStatusName
            ////        }).ToList()
            ////    });
            ////}

            var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();

            var cashOutList = await _sportsbookRepository.GetCashOut(transactions, selections, betBoosted);

            foreach (var bet in transactions)
            {
                var cashOut = cashOutList.Result.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
                var isBoosted = betBoosted.Any(b => b == bet.BetId);
                result.Add(new BetHistoryDTO
                {
                    BetId = bet.BetId.ToString(),
                    Stake = bet.Stake,
                    Odds = bet.Odds,
                    Payout = bet.Payout,
                    WinLoss = bet.WinLoss,
                    BetStatusId = bet.BetStatusId,
                    BetStatusName = bet.BetStatusName,
                    TransactionDate = bet.BetDate,
                    Commission = 0,
                    CurrencyCode = bet.CurrencyCode,
                    OddsType = bet.OddsTypeName,
                    CashOutStake = cashOut.CashOutStake,
                    CashOutStatusId = (int)cashOut.CashOutStatus,
                    CashOutStatusName = cashOut.CashOutStatus.ToString(),
                    StakeTax = bet.StakeTax,
                    WinningsTax = bet.WinningsTax,
                    TaxedStake = bet.BetStatusId == (int)BetStatus.Running ? 0.00m : bet.TaxedStake,
                    TaxedWinnings = bet.TaxedWinnings,
                    ParlayBonus = bet.ParlayBonus,
                    ParlayBonusWinnings = bet.ParlayBonusWinnings,
                    StakeReal = bet.StakeReal,
                    StakeRealPayout = bet.StakeRealPayout,
                    StakeBonus = bet.StakeBonus,
                    StakeBonusPayout = bet.StakeBonusPayout,
                    IsFreeBet = bet.IsFreeBet,
                    IsBoosted = isBoosted,
                    Selections = filteredSelections.Select(s => new BetSelectionHistoryDTO
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        FixtureId = s.FixtureId.ToString(),
                        FixtureName = s.FixtureName,
                        MarketId = s.MarketId.ToString(),
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId.ToString(),
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        BetScore = s.BetScore,
                        SettlementScore = s.SettlementScore,
                        BetSelectionId = s.BetSelectionId.ToString(),
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatusId = s.FixtureStatusId,
                        FixtureStatusName = s.FixtureStatusName,
                        MarketTemplateId = s.MarketTemplateId,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        SelectionTemplateId = s.SelectionTemplateId,
                        BetSelectionStatusId = s.BetSelectionStatusId,
                        BetSelectionStatusName = s.BetSelectionStatusName,
                        Probability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.Probability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.Probability).FirstOrDefault(),
                        WonProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.WonProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.WonProbability).FirstOrDefault(),
                        LostProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.LostProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.LostProbability).FirstOrDefault(),
                        VoidProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.VoidProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.VoidProbability).FirstOrDefault(),
                        HalfWonProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfWonProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfWonProbability).FirstOrDefault(),
                        HalfLostProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfLostProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfLostProbability).FirstOrDefault()
                    }).ToList()
                });
            }

            return new BaseResponseDTO<BetHistoryResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetHistoryResponseDTO
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = Convert.ToInt32(rowCount),
                    TotalCommission = 0,
                    TotalWinLoss = winLoss,
                    TotalWLC = winLoss,
                    Bets = result.OrderByDescending(o => o.TransactionDate).ToList()
                }
            };
        }
        public async Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(BetConfigRequestDTO request)
        {
            var result = new BetConfigFEResponseDTO();
            var clientId = new Guid(request.ClientId);
            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == clientId && x.CurrencyCode == request.CurrencyCode).FirstOrDefault();

            DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            TaxConfig taxConfig = !string.IsNullOrEmpty(betConfig.TaxConfig) ? JsonSerializer.Deserialize<TaxConfig>(betConfig.TaxConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new TaxConfig();
            PriceboostConfig priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();

            result.DefaultSportConfig = new DefaultSportConfigFE()
            {
                MinStake = GetMinStake(defaultSportConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode),
                MaxStake = defaultSportConfig.MaxStake,
                MaxPerGame = defaultSportConfig.StakeLimitFixture
            };

            result.OverrideSportConfig = new List<OverrideSportConfigFE>();

            foreach (var overRide in overrideSportConfig)
            {
                OverrideSportConfigFE newOverrideSport = new OverrideSportConfigFE();

                newOverrideSport.SportId = overRide.SportId;
                newOverrideSport.SportName = overRide.SportName;
                newOverrideSport.MinStake = GetMinStake(overRide.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode);
                newOverrideSport.MaxStake = overRide.MaxStake;
                newOverrideSport.MaxPerGame = overRide.StakeLimitFixture;

                result.OverrideSportConfig.Add(newOverrideSport);
            }

            result.Parlayconfig = new ParlayConfigFE()
            {
                MinStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode),
                MaxStake = parlayConfig.MaxStake,
                MaxPayout = parlayConfig.MaxPayout,
                ParlayBonusMinOdds = parlayConfig.ParlayBonusMinOdds,
                ParlayBonusMaxWinnings = parlayConfig.ParlayBonusMaxWinnings,
                ParlayBonusConfig = parlayConfig.ParlayBonusConfig == null ? new List<ParlayBonusConfigFE>() : parlayConfig.ParlayBonusConfig.Select(s => new ParlayBonusConfigFE { ParlayBonus = s.ParlayBonus, SelectionCount = s.SelectionCount }).ToList()
            };

            result.TaxConfig = new TaxConfig
            {
                StakeTax = taxConfig.StakeTax,
                WinningsTax = taxConfig.WinningsTax
            };

            result.PriceboostConfig = priceboostConfig;

            return new BaseResponseDTO<BetConfigFEResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }
        public async Task<BaseResponseDTO<BetModifyResponseDTO>> CancelBets(BetModifyRequestDTO request)
        {
            BetModifyResponseDTO response = new BetModifyResponseDTO()
            {
                Data = request.BetIds.Select(m => new BetModifyResponse
                {
                    BetId = m,
                    IsSuccess = true
                }).ToList(),
                IsSuccess = false
            };

            try
            {
                var betIds = response.Data.Select(m => Guid.Parse(m.BetId)).ToList();
                var bets = _bettingContext.Bet.Where(x => betIds.Contains(x.BetId)).ToList();
                List<BetCancel> betList = new List<BetCancel>();

                #region Validate Request

                var settledBetStatus = new int[] { (int)BetStatus.Won, (int)BetStatus.Lost, (int)BetStatus.HalfWon, (int)BetStatus.HalfLost, (int)BetStatus.Rejected };
                var unsettledBetStatus = new int[] { (int)BetStatus.Waiting };

                foreach (var data in response.Data)
                {
                    var betId = Guid.Parse(data.BetId);
                    var bet = bets.SingleOrDefault(m => m.BetId == betId);

                    betList.Add(new BetCancel
                    {
                        BetId = betId,
                        Reason = request.Reason
                    });

                    data.OldBetStatus = bet != null ? bet.BetStatusName : string.Empty;

                    if (bet == null)
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet ID not found";
                    }
                    else if (bet.BetStatusId == (int)BetStatus.Waiting)
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet Cancellation not allowed on bets pending for acceptance by MTS";
                    }
                    //else if (bet.BetStatusId == (int)BetStatus.CashedOut)
                    //{
                    //    data.IsSuccess = false;
                    //    data.Message = "Bet Cancellation not allowed on Cashed Out bets";
                    //}
                    else if (bet.BetStatusId == (int)BetStatus.Rejected)
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet Cancellation not allowed on rejected bets";
                    }
                    //else if (bet.BetStatusId != (int)BetStatus.Running)
                    //{
                    //    data.IsSuccess = false;
                    //    data.Message = "Bet Cancellation cannot be processed on highlighted Bet ID. Either Payment Transfer or Settlement has been made after bet settled or player wallet is 0.00.";
                    //}
                    //else if (!unsettledBetStatus.Contains(bet.BetStatusId) && settledBetStatus.Contains(bet.BetStatusId))
                    //{
                    //    data.IsSuccess = false;
                    //    data.Message = "Bet ID is already settled and can no longer be cancelled.";
                    //}
                    //else if (bet.BetStatusId == (int)BetStatus.Void)
                    //{
                    //    data.IsSuccess = false;
                    //    data.Message = "Bet Reference ID entered has been cancelled previously.";
                    //}
                    else if (bet.BetStatusId == (int)BetStatus.Cancelled)
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet Cancellation not allowed on Cancelled bets";
                    }
                }

                // check if there are any bets that failed the validation
                // if yes, then don't proceed to the next step
                if (response.Data.Count(m => !m.IsSuccess) > 0)
                    return new BaseResponseDTO<BetModifyResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = response
                    };

                #endregion                

                //_messageBroker.Publish(new OnBOBetCancellaton
                //{
                //    Bets = betList
                //}, "OnBetBackOfficeCancellation", "r.bet.settlement", Guid.NewGuid().ToString().ToUpper());

                var payload = new OnBOBetCancellaton
                {
                    Bets = betList
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnBetBackOfficeCancellation",
                    RoutingKey = "r.bet.settlement"
                });

                bool success = response.Data.Any(m => m.IsSuccess);
                response.IsSuccess = success;
                return new BaseResponseDTO<BetModifyResponseDTO>
                {
                    StatusCode = success ? StatusCode.Success : StatusCode.Error,
                    Message = success ? String.Empty : StatusCode.Error.ToString(),
                    Result = response
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return new BaseResponseDTO<BetModifyResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message,
                    Result = response
                };
            }
        }

        public async Task<BaseResponseDTO<ManualSettlementResponseDTO>> ManualSettlement(ManualBetSettlementRequest request)
        {
            ManualSettlementResponseDTO response = new ManualSettlementResponseDTO()
            {
                Data = request.BetData.Select(m => new ManualBetResponse
                {
                    BetDetails = new ManualBetDetailsDTO()
                    {
                        BetId = Guid.Parse(m.BetId),
                        Reason = m.Reason,
                        BetStatusId = (int)m.NewStatus,
                        BetStatusName = m.NewStatus.ToString(),
                    },
                    IsSuccess = true
                }).ToList(),
                IsSuccess = false
            };

            try
            {
                var betIds = response.Data.Select(m => m.BetDetails.BetId).ToList();
                var bets = _bettingContext.Bet.Where(x => betIds.Contains(x.BetId)).ToList();
                var betSelections = _bettingContext.BetSelection.Where(x => betIds.Contains(x.BetId)).ToList();
                var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();
                
                List<BetManualSettlement> betList = new List<BetManualSettlement>();

                #region Validate Request
                var rejectedStatus = new List<int> { (int)BetStatus.Rejected, (int)BetStatus.Cancelled, (int)BetStatus.CashedOut };
                var statuses = new List<int> { (int)BetStatus.Running, (int)BetStatus.Won, (int)BetStatus.Lost, (int)BetStatus.HalfLost, (int)BetStatus.HalfWon, (int)BetStatus.Void };

                foreach (var data in response.Data)
                {
                    var betId = data.BetDetails.BetId;
                    var bet = bets.SingleOrDefault(m => m.BetId == betId);

                    betList.Add(new BetManualSettlement
                    {
                        BetId = betId,
                        Reason = data.BetDetails.Reason,
                        NewStatus = (BetStatus)Enum.Parse(typeof(BetStatus), data.BetDetails.BetStatusName),
                    });

                    data.OldBetStatus = bet != null ? bet.BetStatusName : string.Empty;

                    if (bet == null)
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet ID not found";
                    }
                    else if (rejectedStatus.Contains(bet.BetStatusId))
                    {
                        // check if status is not on the list
                        data.IsSuccess = false;
                        data.Message = "Rejected, Cancelled and Cashout bets cannot be resettled!";
                    }
                    else if (!statuses.Contains(bet.BetStatusId))
                    {
                        data.IsSuccess = false;
                        data.Message = "Bet settlement not allowed";
                    }
                    else
                    {
                        //update settlement score
                        foreach (var selection in betSelections)
                        {
                            var settlementScoreResult = (await _sportsbookRepository.GetBetSettlementScore(
                                    new BetSettlementScoreRequestDTO()
                                    {
                                        FixtureId = selection.FixtureId,
                                    })).Result;

                            var settlementScore = settlementScoreResult == null ? string.Empty : settlementScoreResult.Score;
                            selection.SettlementScore = string.IsNullOrEmpty(settlementScore) ? selection.SettlementScore : settlementScore;
                        }

                        var newBet = data.BetDetails;
                        newBet.BetDate = bet.BetDate;
                        newBet.BetDateOffset = bet.BetDateOffset;
                        newBet.ClientId = bet.ClientId;
                        newBet.ClientCode = bet.ClientCode;
                        newBet.ClientIdRef = bet.ClientIdRef;
                        newBet.ClientName = bet.ClientName;
                        newBet.CurrencyCode = bet.CurrencyCode;
                        newBet.IsFreeBet = bet.IsFreeBet;
                        newBet.BetDate = bet.BetDate;
                        newBet.BetDateOffset = bet.BetDateOffset;
                        newBet.OddsTypeId = bet.OddsTypeId;
                        newBet.OddsTypeName = bet.OddsTypeName;
                        newBet.Stake = bet.Stake;
                        newBet.Odds = bet.Odds;
                        newBet.SettlementDate = bet.SettlementDate;
                        newBet.UserId = bet.UserId;
                        newBet.TaxedStake = bet.TaxedStake;
                        newBet.Channel = bet.Channel;
                        newBet.CompanyShare = bet.CompanyShare;
                        newBet.Endpoint = bet.Endpoint;
                        newBet.ExchangeRate = bet.ExchangeRate;
                        newBet.IPAddress = bet.IPAddress;
                        newBet.IsCashout = bet.IsCashout;
                        newBet.IsResettled = bet.IsResettled;
                        newBet.LanguageCode = bet.LanguageCode;
                        newBet.LimitId = bet.LimitId;
                        newBet.OperatorToken = bet.OperatorToken;
                        newBet.ParlayBonus = bet.ParlayBonus;
                        newBet.ParlayBonusWinnings = bet.ParlayBonusWinnings;
                        newBet.Payout = bet.Payout;
                        newBet.SettlementDateOffset = bet.SettlementDateOffset;
                        newBet.StakeBonus = bet.StakeBonus;
                        newBet.StakeBonusPayout = bet.StakeBonusPayout;
                        newBet.StakeReal = bet.StakeReal;
                        newBet.StakeRealPayout = bet.StakeRealPayout;
                        newBet.StakeTax = bet.StakeTax;
                        newBet.TaxedWinnings = bet.TaxedWinnings;
                        newBet.TrueStake = bet.TrueStake;
                        newBet.WinLoss = bet.WinLoss;
                        newBet.WinningsTax = bet.WinningsTax;
                        newBet.IsBoosted = betBoosted.Any(b => b == bet.BetId);

                        newBet.Selections = betSelections.Where(m => m.BetId == betId).Select(s => new ManualBetSelectionDetailsDTO()
                        {
                            SportId = s.SportId,
                            SportName = s.SportName,
                            LeagueId = s.LeagueId,
                            LeagueName = s.LeagueName,
                            FixtureId = s.FixtureId.ToString(),
                            FixtureName = s.FixtureName,
                            MarketId = s.MarketId.ToString(),
                            MarketName = s.MarketName,
                            Line = s.Line,
                            SelectionId = s.SelectionId.ToString(),
                            SelectionName = s.SelectionName,
                            Odds = s.Odds,
                            BetScore = s.BetScore,
                            SettlementScore = s.SettlementScore,
                            BetSelectionId = s.BetSelectionId,
                            FixtureStartDate = s.FixtureStartDate,
                            FixtureStatusId = s.FixtureStatusId,
                            FixtureStatusName = s.FixtureStatusName,
                            MarketTemplateId = s.MarketTemplateId,
                            SegmentId = s.SegmentId,
                            SegmentName = s.SegmentName,
                            SelectionTemplateId = s.SelectionTemplateId,
                            BetSelectionStatusId = s.BetSelectionStatusId,
                            BetSelectionStatusName = s.BetSelectionStatusName,
                            BetId = s.BetId,
                            CorrelationId = s.CorrelationId,
                            DeadHeatFactor = s.DeadHeatFactor,
                            FixtureSpecifier = s.FixtureSpecifier,
                            FixtureTypeId = s.FixtureTypeId,
                            FixtureTypeName = s.FixtureTypeName,
                            HalfLostProbability = s.HalfLostProbability,
                            HalfWonProbability = s.HalfWonProbability,
                            IsResettled = s.IsResettled,
                            IsSettled = s.IsSettled,
                            LostProbability = s.LostProbability,
                            Probability = s.LostProbability,
                            SelectionSpecifier = s.SelectionSpecifier,
                            VoidProbability = s.VoidProbability,
                            WonProbability = s.WonProbability,
                            UserId = s.UserId,
                        }).ToList();
                    }
                }

                // check if there are any bets that failed the validation
                // if yes, then don't proceed to the next step
                if (response.Data.Count(m => !m.IsSuccess) > 0)
                {
                    response.Message = string.Join(";", response.Data.Where(m => !m.IsSuccess).Select(s => s.Message).Distinct());
                    return new BaseResponseDTO<ManualSettlementResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = StatusCode.Error.ToString(),
                        Result = response
                    };
                }

                #endregion                

                var payload = new OnBOBetManualSettlement
                {
                    Bets = betList
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnBetBackOfficeManualSettlement",
                    RoutingKey = "r.bet.settlement"
                });

                bool success = response.Data.Any(m => m.IsSuccess);
                response.IsSuccess = success;
                return new BaseResponseDTO<ManualSettlementResponseDTO>
                {
                    StatusCode = success ? StatusCode.Success : StatusCode.Error,
                    Message = success ? string.Empty : StatusCode.Error.ToString(),
                    Result = response
                };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return new BaseResponseDTO<ManualSettlementResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message,
                    Result = response
                };
            }
        }
        public async Task<BaseResponseDTO<ManualBetDetailsResponseDTO>> GetManualBetDetails(ManualBetDetailsRequest request)
        {
            var betGuids = request.BetIds.Select(s => Guid.Parse(s));

            var selections = _bettingContext.BetSelection.AsNoTracking().Where(s => betGuids.Contains(s.BetId)).ToList();
            
            var selectionBetIds = selections.Select(s => s.BetId).Distinct().ToList();

            var rawAll = _bettingContext.Bet.AsNoTracking().Where(c =>
                                selectionBetIds.Contains(c.BetId)).Select(s => new ManualBetDetailsDTO()
                                {
                                    BetId = s.BetId,
                                    BetStatusId = s.BetStatusId,
                                    BetStatusName = s.BetStatusName,
                                    ClientId = s.ClientId,
                                    ClientCode = s.ClientCode,
                                    ClientIdRef = s.ClientIdRef,
                                    ClientName = s.ClientName,
                                    CurrencyCode = s.CurrencyCode,
                                    IsFreeBet = s.IsFreeBet,
                                    Reason = s.Reason,
                                    BetDate = s.BetDate,
                                    BetDateOffset = s.BetDateOffset,
                                    OddsTypeId = s.OddsTypeId,
                                    OddsTypeName = s.OddsTypeName,
                                    Stake = s.Stake,
                                    Odds = s.Odds,
                                    SettlementDate = s.SettlementDate,
                                    UserId = s.UserId,
                                    TaxedStake = s.TaxedStake,
                                    Channel = s.Channel,
                                    CompanyShare = s.CompanyShare,
                                    Endpoint = s.Endpoint,
                                    ExchangeRate = s.ExchangeRate,
                                    IPAddress = s.IPAddress,
                                    IsCashout = s.IsCashout,
                                    IsResettled = s.IsResettled,
                                    LanguageCode = s.LanguageCode,
                                    LimitId = s.LimitId,
                                    OperatorToken = s.OperatorToken,
                                    ParlayBonus = s.ParlayBonus,
                                    ParlayBonusWinnings = s.ParlayBonusWinnings,
                                    Payout = s.Payout,
                                    SettlementDateOffset = s.SettlementDateOffset,
                                    StakeBonus = s.StakeBonus,
                                    StakeBonusPayout = s.StakeBonusPayout,
                                    StakeReal = s.StakeReal,
                                    StakeRealPayout = s.StakeRealPayout,
                                    StakeTax = s.StakeTax,
                                    TaxedWinnings = s.TaxedWinnings,
                                    TrueStake = s.TrueStake,
                                    WinLoss = s.WinLoss,
                                    WinningsTax = s.WinningsTax,
                                }
                           );

            var result = rawAll.ToList();

            if (result == null || !result.Any())
            {
                return new BaseResponseDTO<ManualBetDetailsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new ManualBetDetailsResponseDTO
                    {
                        Bets = new List<ManualBetDetailsDTO>()
                    }
                };
            }

            var betIds = result.Select(s => s.BetId).Distinct().ToList();
            
            var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();

            foreach (var bet in result)
            {
                var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();

                bet.IsBoosted = betBoosted.Any(b => b == bet.BetId);
                bet.Selections = filteredSelections.Select(s => new ManualBetSelectionDetailsDTO
                {
                    SportId = s.SportId,
                    SportName = s.SportName,
                    LeagueId = s.LeagueId,
                    LeagueName = s.LeagueName,
                    FixtureId = s.FixtureId.ToString(),
                    FixtureName = s.FixtureName,
                    MarketId = s.MarketId.ToString(),
                    MarketName = s.MarketName,
                    Line = s.Line,
                    SelectionId = s.SelectionId.ToString(),
                    SelectionName = s.SelectionName,
                    Odds = s.Odds,
                    BetScore = s.BetScore,
                    SettlementScore = s.SettlementScore,
                    BetSelectionId = s.BetSelectionId,
                    FixtureStartDate = s.FixtureStartDate,
                    FixtureStatusId = s.FixtureStatusId,
                    FixtureStatusName = s.FixtureStatusName,
                    MarketTemplateId = s.MarketTemplateId,
                    SegmentId = s.SegmentId,
                    SegmentName = s.SegmentName,
                    SelectionTemplateId = s.SelectionTemplateId,
                    BetSelectionStatusId = s.BetSelectionStatusId,
                    BetSelectionStatusName = s.BetSelectionStatusName,
                    BetId = s.BetId,
                    CorrelationId = s.CorrelationId,
                    DeadHeatFactor = s.DeadHeatFactor,
                    FixtureSpecifier = s.FixtureSpecifier,
                    FixtureTypeId = s.FixtureTypeId,
                    FixtureTypeName = s.FixtureTypeName,
                    HalfLostProbability = s.HalfLostProbability,
                    HalfWonProbability = s.HalfWonProbability,
                    IsResettled = s.IsResettled,
                    IsSettled = s.IsSettled,
                    LostProbability = s.LostProbability,
                    Probability = s.LostProbability,
                    SelectionSpecifier = s.SelectionSpecifier,
                    VoidProbability = s.VoidProbability,
                    WonProbability = s.WonProbability,
                }).ToList();
            }

            return new BaseResponseDTO<ManualBetDetailsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new ManualBetDetailsResponseDTO
                {
                    Bets = result.OrderByDescending(o => o.SettlementDate).ToList()
                }
            };
        }

        public async Task<BaseResponseDTO<ResendSettlementResponseDTO>> ResendSettlement(ResendSettlementMessageRequestDTO request)
        {
            foreach (var message in request.Messages)
            {
                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                { 
                    MessageType = message.MessageType,
                    Payload = message.Info,
                    CorrelationId = message.CorrelationId,
                    RoutingKey = "r.bet.operator"
                });
            }

            return new BaseResponseDTO<ResendSettlementResponseDTO>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString().Replace('_', ' '),
                Result = new ResendSettlementResponseDTO
                {
                    Data = request.Messages.Select(s => new ResendSettlementResponse
                    {
                        BetId = s.BetId,
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace('_', ' '),
                    }).ToList()
                }
            };
        }

        public async Task<BaseResponseDTO<BetCashOutResponseDTO>> CashOutBet(CashoutBetRequestDTO request)
        {
            var bet = _bettingContext.Bet.AsNoTracking().Where(c => c.BetId == request.BetId).FirstOrDefault();

            if (bet == null || bet.BetStatusId != (int)BetStatus.Running)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var hasExistingBetPriceBoost = _bettingContext.BetPriceBoost.AsNoTracking().Any(b => b.BetId == request.BetId && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled);

            if (hasExistingBetPriceBoost == true)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var betSelections = _bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).AsNoTracking().ToList();

            if (betSelections.Count == 0)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var result = await _sportsbookRepository.GetCashOut(new List<Bet> { bet }, betSelections, new List<Guid>());

            var cashOut = result.Result.Where(c => c.BetId == bet.BetId && c.CashOutStatus == CashOutStatus.Available).FirstOrDefault();

            if (cashOut == null)
            {
                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            if (Math.Round(request.CashOutStake, 2) != Math.Round(cashOut.CashOutStake, 2))
            {
                var requestCashOutStake = request.CashOutStake % 1 == 0 ? request.CashOutStake.ToString().TrimEnd('0').TrimEnd('.') : request.CashOutStake.ToString().TrimEnd('0').TrimEnd('.');
                var resultCashOutStake = cashOut.CashOutStake % 1 == 0 ? cashOut.CashOutStake.ToString().TrimEnd('0').TrimEnd('.') : cashOut.CashOutStake.ToString().TrimEnd('0').TrimEnd('.');

                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Stake_Changed_From_X_To_Y,
                    Message = StatusCode.CashOut_Stake_Changed_From_X_To_Y.ToString().Replace('_', ' ').Replace("X", requestCashOutStake).Replace("Y", resultCashOutStake),
                    Result = new BetCashOutResponseDTO
                    {
                        BetId = bet.BetId.ToString().ToUpper(),
                        BetStatus = (BetStatus)bet.BetStatusId,
                        UserId = bet.UserId,
                        CashOutStake = cashOut.CashOutStake
                    }
                };
            }
            else
            {
                var payload = new OnCashoutBetSettlement
                {
                    BetId = request.BetId,
                    UserId = bet.UserId,
                    ClientCode = bet.ClientCode,
                    ClientId = bet.ClientId.ToUpper(),
                    CashoutType = CashoutType.Full,
                    CashOutStake = cashOut.CashOutStake,
                    TaxedStake = cashOut.TaxedStake,
                    TaxedWinnings = cashOut.TaxedWinnings,
                    NonTaxedPayout = cashOut.NonTaxedPayout,
                    HasLive = cashOut.HasLive,
                    Endpoint = bet.Endpoint,
                    Probabilities = cashOut.Probabilities.Select(s => new CashedOutProbabilities
                    {
                        BetSelectionId = s.BetSelectionId,
                        Probability = s.Probability,
                        WonProbability = s.WonProbability,
                        LostProbability = s.LostProbability,
                        VoidProbability = s.VoidProbability,
                        HalfWonProbability = s.HalfWonProbability,
                        HalfLostProbability = s.HalfLostProbability
                    }).ToList()
                };

                _rabbitMQRepository.PublishViaHttp(new MessageRequestDTO
                {
                    Payload = JsonSerializer.Serialize(payload),
                    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
                    MessageType = "OnBetFullCashOut",
                    RoutingKey = "r.bet.cashout"
                });

                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetCashOutResponseDTO
                    {
                        BetId = bet.BetId.ToString().ToUpper(),
                        BetStatus = (BetStatus)bet.BetStatusId,
                        UserId = bet.UserId,
                        CashOutStake = cashOut.CashOutStake
                    }
                };
            }
        }
        public async Task<BaseResponseDTO<CashoutStateResponseDTO>> GetCashOut(CashoutStateRequestDTO request)
        {
            var bet = _bettingContext.Bet.AsNoTracking().Where(c => c.BetId == request.BetId).FirstOrDefault();

            if (bet == null || bet.BetStatusId != (int)BetStatus.Running)
            {
                return new BaseResponseDTO<CashoutStateResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var hasExistingBetPriceBoost = _bettingContext.BetPriceBoost.AsNoTracking().Any(b => b.BetId == request.BetId && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled);

            if (hasExistingBetPriceBoost == true)
            {
                return new BaseResponseDTO<CashoutStateResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var betSelections = _bettingContext.BetSelection.Where(c => c.BetId == bet.BetId).AsNoTracking().ToList();

            if (betSelections.Count == 0)
            {
                return new BaseResponseDTO<CashoutStateResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var result = await _sportsbookRepository.GetCashOut(new List<Bet> { bet }, betSelections, new List<Guid>());

            var cashOut = result.Result.Where(c => c.BetId == bet.BetId && c.CashOutStatus == CashOutStatus.Available).FirstOrDefault();

            if (cashOut == null)
            {
                return new BaseResponseDTO<CashoutStateResponseDTO>
                {
                    StatusCode = StatusCode.CashOut_Is_Unavailable,
                    Message = StatusCode.CashOut_Is_Unavailable.ToString().Replace('_', ' ')
                };
            }

            var payload = new CashoutStateResponseDTO
            {
                BetId = request.BetId,
                CashoutType = CashoutType.Full,
                CashOutStake = cashOut.CashOutStake,
                TaxedStake = cashOut.TaxedStake,
                TaxedWinnings = cashOut.TaxedWinnings,
                NonTaxedPayout = cashOut.NonTaxedPayout,
                Probabilities = cashOut.Probabilities.Select(s => new CashedOutProbabilities
                {
                    BetSelectionId = s.BetSelectionId,
                    Probability = s.Probability,
                    WonProbability = s.WonProbability,
                    LostProbability = s.LostProbability,
                    VoidProbability = s.VoidProbability,
                    HalfWonProbability = s.HalfWonProbability,
                    HalfLostProbability = s.HalfLostProbability
                }).ToList()
            };

            return new BaseResponseDTO<CashoutStateResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = payload
            };
        }
        private decimal GetMinStake(decimal minStake, decimal exchangeRate, decimal companyShare, string currencyCode)
        {
            List<decimal> minimumStake = new List<decimal>();
            var highCurrencies = new List<string> { "KRW", "VND", "KHR", "IDR", "MMK" };

            //1. Reference Bet
            //decimal referenceBet = 1;
            //if (currencyCode == CurrencyCode.EUR.ToString())
            //{
            //    minimumStake.Add(1);
            //}
            //else
            //{
            //    minimumStake.Add(0.02m);
            //    referenceBet = exchangeRate * 0.0001m;
            //}


            //2. High Currency add 1000 default
            if (highCurrencies.Contains(currencyCode))
                minimumStake.Add(1000m);

            //3 Get Min from betconfig
            minimumStake.Add(minStake);

            //4 Reference Bet / Company Share
            //if (companyShare > 0)
            //    minimumStake.Add(Math.Truncate(referenceBet / companyShare) + 1);

            //Get Max decimal value
            return minimumStake.Max();
        }
        private decimal GetMaxStakeForSingleBetWithoutMTS(decimal maxStakeBackOffice, decimal maxStakeTier, decimal exchangeRate, decimal odds)
        {
            List<decimal> maximumStake = new List<decimal>();

            maximumStake.Add(maxStakeBackOffice);

            if (maxStakeTier > 0)
                maximumStake.Add((maxStakeTier * exchangeRate));

            if (maxStakeTier > 0)
                maximumStake.Add(((maxStakeTier * exchangeRate) / (odds - 1)));

            return maximumStake.Min();
        }
        private decimal GetMaxStakeForParlayBetWithoutMTS(decimal maxStakeBackOffice, decimal parlayMaxPayout, decimal odds)
        {
            List<decimal> maximumStake = new List<decimal>();

            maximumStake.Add(maxStakeBackOffice);
            maximumStake.Add((parlayMaxPayout / odds));

            return maximumStake.Min();
        }
        private decimal GetMaxStakeForSingleBetWithMTS(decimal maxStakeBackOffice, decimal maxStakeTier, decimal maxStakeMTS, decimal exchangeRate, decimal odds, decimal companyShare)
        {
            List<decimal> maximumStake = new List<decimal>();

            if (maxStakeMTS == 0)
                maximumStake.Add(GetMaxStakeForSingleBetWithoutMTS(maxStakeBackOffice, maxStakeTier, exchangeRate, odds));
            else
                maximumStake.Add(maxStakeMTS);

            return maximumStake.Min();
        }
        private decimal GetMaxStakeForParlayBetWithMTS(decimal maxStakeBackOffice, decimal maxStakeMTS, decimal parlayMaxPayout, decimal odds, decimal companyShare)
        {
            List<decimal> maximumStake = new List<decimal>();

            if (maxStakeMTS == 0)
                maximumStake.Add(GetMaxStakeForParlayBetWithoutMTS(maxStakeBackOffice, parlayMaxPayout, odds));
            else
                maximumStake.Add(maxStakeMTS);

            return maximumStake.Min();
        }
        private decimal RoundUpDecimalValue(decimal value)
        {
            return (value % 1m != 0) ? Math.Truncate(value) + 1 : value;
        }

        public async Task<BetConfigLeagueResponse> GetTierMaxStake(BetConfigLeagueRequestDTO request)
        {
            BetConfigLeagueResponse result = new BetConfigLeagueResponse();
            int TierId = 2;
            var league = await _bettingContext.League.Where(a => a.SportId == request.SportId && a.LeagueId == request.LeagueId && a.SegmentId == request.SegmentId).FirstOrDefaultAsync();
            List<int> eSports = new List<int> { 1001, 1002, 1003, 1005, 1055, 1058 };
            List<int> virtualSports = new List<int> { 107, 109, 110, 111, 132, 133, 134, 139, 140, 152, 154, 156, 158, 159, 160, 161, 162, 164, 166, 167, 168 };

            if (league == null)
            {
                if (eSports.Contains(request.SportId))
                    TierId = 1;
                if (virtualSports.Contains(request.SportId))
                    TierId = 3;
            }
            else
            {
                TierId = league.Tier;
            }

            var tier = _bettingContext.Tier.Find(TierId);
            result.MaxStake = tier.MaxStake;
            result.MaxPerGame = tier.MaxPerGame;

            return result;
        }

    }
}
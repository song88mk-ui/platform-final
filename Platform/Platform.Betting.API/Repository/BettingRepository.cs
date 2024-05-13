using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
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
        private readonly IMemCache _memCache;

        private readonly IMTSAuthRepository _mtsAuthRepository;
        private readonly IMTSMaxStakeRepository _mtsMaxStakeRepository;
        private readonly IMTSProbabilityRepository _mtsProbabilityRepository;
        private readonly ISportsbookRepository _sportsbookRepository;
        private readonly IRabbitMQRepository _rabbitMQRepository;

        private readonly TelemetryClient _telemetryClient;

        public BettingRepository(BettingContext bettingContext, IConfiguration configuration, IMemCache memCache, IHttpClientFactory httpClientFactory, IMTSAuthRepository mtsAuthRepository, IMTSMaxStakeRepository mtsMaxStakeRepository, IMTSProbabilityRepository mtsProbabilityRepository, ISportsbookRepository sportsbookRepository, IRabbitMQRepository rabbitMQRepository, TelemetryClient telemetryClient)
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
            _memCache = memCache;
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

            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == Guid.Parse(request.ClientId) && x.CurrencyCode == request.CurrencyCode.ToString()).FirstOrDefault();
            #region Vertical Setting
            //if (request.IsAgent)
            //{
            //    var userBetConfig = await GetUserBetConfig(new UserBetConfigDetailsRequestDTO() { ClientId = request.ClientId, UserId = request.UserId });

            //    if (userBetConfig.StatusCode != StatusCode.Success)
            //    {
            //        return new BaseResponseDTO<BetResponseDTO>()
            //        {
            //            StatusCode = userBetConfig.StatusCode,
            //            Message = userBetConfig.Message
            //        };
            //    }

            //    var message = "You are currently not allowed to place a bet. Please contact your agent for more information";
            //    if (request.Selections.Count() > 1)
            //    {
            //        if (!userBetConfig.Result.IsParlayEnabled)
            //        {
            //            return new BaseResponseDTO<BetResponseDTO>()
            //            {
            //                StatusCode = StatusCode.Not_Authorized,
            //                Message = message
            //            };
            //        }
            //    }
            //    else
            //    {
            //        if (!userBetConfig.Result.IsCricketEnabled)
            //        {
            //            if (request.Selections.Any(x => x.SportId == 21))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }
            //        if (!userBetConfig.Result.IsSoccerEnabled)
            //        {
            //            if (request.Selections.Any(x => x.SportId == 1))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }

            //        if (!userBetConfig.Result.IsSportsEnabled)
            //        {
            //            var excludedSports = new List<int>() { 21, 1 };
            //            if (request.Selections.Any(x => excludedSports.Contains(x.SportId)))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }
            //    }
            //}
            #endregion
            decimal minStake = 0;
            decimal maxStake = 0;
            decimal maxPerGame = 0;
            decimal stakeFixtureLimit = 0;
            decimal parlayBonus = 0;
            var odds = request.IsBetBuilder ? request.Odds : request.Selections.Select(s => s.DecimalOdds).Aggregate((a, b) => a * b);
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
                        StatusCode = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake.ToString().Replace('_', ' ')
                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var stakeFixtureLimitResult = _bettingContext.StakeLimitFixture.Find($"{request.ClientCode}-{request.UserId}", request.Selections.First().FixtureId);

                if (stakeFixtureLimitResult != null)
                {
                    var remainingFixtureBet = stakeFixtureLimitResult.Remaining - request.Stake;

                    if (remainingFixtureBet < 0)
                    {
                        return new BaseResponseDTO<BetResponseDTO>
                        {
                            StatusCode = StatusCode.Maximum_Bet_Amount_For_The_Game_Has_Been_Reached,
                            Message = StatusCode.Maximum_Bet_Amount_For_The_Game_Has_Been_Reached.ToString().Replace('_', ' ')
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
                        StatusCode = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
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

                if (request.IsBetBuilder == false && hasVirtualSport == false && request.Selections.Count >= 3 && parlayConfig.ParlayBonusConfig != null)
                {
                    var selectionCount = request.Selections.Where(c => c.DecimalOdds >= parlayConfig.ParlayBonusMinOdds).Count();
                    if (selectionCount >= 3)
                    {
                        var parlayBonusConfig = parlayConfig.ParlayBonusConfig.FirstOrDefault(c => c.SelectionCount == selectionCount);

                        if (parlayBonusConfig != null)
                            parlayBonus = parlayBonusConfig.ParlayBonus;
                    }
                }
            }

            var betData = new Bet();
            //var betSelectionData = new List<BetSelection>();
            var betSelectionData = new List<BetSelectionMessageExtenstion>();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    Guid betId;

                    var totalOdds = request.IsBetBuilder ? request.Odds : request.Selections.Select(s => s.DecimalOdds).Aggregate((a, b) => a * b);
                    var totalOddsRef = request.Selections.Select(s => s.OddsRef).Aggregate((a, b) => a * b);
                    var selectionSports = request.Selections.Select(s => s.SportId).Distinct().ToList();

                    var stakeTax = taxConfig.StakeTax;

                    if (stakeTax > 0)
                    {
                        var excludedTaxedSport = !string.IsNullOrEmpty(taxConfig.ExcludeStakeSportTypes) ? taxConfig.ExcludeStakeSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedTaxedSport.Count == selectionSports.Count)
                            stakeTax = 0m;
                    }

                    var taxedStake = 0m; // request.Stake - (request.Stake / (1 + stakeTax));
                    var finalStake = request.Stake - taxedStake;
                    var potentialPayout = finalStake * totalOdds;

                    // Malay and Indo
                    if (request.Selections.Count == 1)
                    {
                        var selection = request.Selections.FirstOrDefault();

                        // malay code here
                        if ((int)request.OddsType == 1 && selection.MalaysianOdds < 0 && selection.IsBinary == true)
                        {
                            var currentStake = finalStake;
                            finalStake = Math.Abs(finalStake * Math.Round(selection.MalaysianOdds, 2));
                            potentialPayout = currentStake + finalStake;
                        }

                        // indo code here
                        if ((int)request.OddsType == 3 && selection.IndonesianOdds < 0 && selection.IsBinary == true)
                        {
                            var currentStake = finalStake;
                            finalStake = Math.Abs(finalStake * Math.Round(selection.IndonesianOdds, 2));
                            potentialPayout = currentStake + finalStake;
                        }

                        // hk code here
                    }

                    var winningsTax = taxConfig.WinningsTax;

                    if (winningsTax > 0)
                    {
                        var excludedWinningSport = !string.IsNullOrEmpty(taxConfig.ExcludeWinningsSportTypes) ? taxConfig.ExcludeWinningsSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedWinningSport.Count == selectionSports.Count)
                            winningsTax = 0m;
                    }

                    var betType = GetBetType(request.Selections, request.IsBoosted, request.IsBetBuilder);

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = finalStake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = potentialPayout,
                        WinLoss = 0m,
                        BetStatusId = (int)BetStatus.Waiting,
                        BetStatusName = BetStatus.Waiting.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExchangeRate,
                        IPAddress = request.IsV4 ? request.IPAddressV4 : request.IPAddressV6,
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
                        IsFreeBet = false,
                        BetTypeId = (int)betType,
                        BetTypeName = betType.ToString(),
                        OddsRef = totalOddsRef,
                        IsAgent = request.IsAgent
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
                        IsFreeBet = betEntity.IsFreeBet,
                        BetTypeId = betEntity.BetTypeId,
                        BetTypeName = betEntity.BetTypeName,
                        OddsRef = betEntity.OddsRef,
                        IsAgent = betEntity.IsAgent
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
                            Odds = selection.DecimalOdds,
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
                            DeadHeatFactor = 1m,
                            IsBinary = selection.IsBinary
                        }).Entity;

                        //betSelectionData.Add(betSelectionEntity);
                        betSelectionData.Add(new BetSelectionMessageExtenstion()
                        {
                            BetSelection = betSelectionEntity,
                            MarketSelectionCoumt = selection.MarketSelectionCount
                        });

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
                            DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                            IsBinary = betSelectionEntity.IsBinary
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

                    betData.IPAddress = request.IPAddressV4;

                    var payload = new OnBetPlacement
                    {
                        Bet = betData,
                        BetSelections = betSelectionData,
                        IsBoosted = request.IsBoosted,
                        IsBetBuilder = request.IsBetBuilder,
                        BonusId = request.BonusId,
                        Odds = request.Odds,
                        UserAgent = request.UserAgent,
                        IsOGClient = request.IsOGClient
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
                    Odds = betSelection.DecimalOdds
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

            var mtsMaxStake = await _mtsMaxStakeRepository.GetMTSMaxStake(mtsAuthToken.Result.Token, mtsConfigRequest, request.CompanyShare, request.IsBetBuilder, request.Odds);

            if (mtsMaxStake.StatusCode != StatusCode.Success)
                return await PlaceBetWithoutMTS(request);

            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == Guid.Parse(request.ClientId) && x.CurrencyCode == request.CurrencyCode.ToString()).FirstOrDefault();
            #region Vertical Setting
            //if (request.IsAgent)
            //{
            //    var userBetConfig = await GetUserBetConfig(new UserBetConfigDetailsRequestDTO() { ClientId = request.ClientId, UserId = request.UserId });

            //    if (userBetConfig.StatusCode != StatusCode.Success)
            //    {
            //        return new BaseResponseDTO<BetResponseDTO>()
            //        {
            //            StatusCode = userBetConfig.StatusCode,
            //            Message = userBetConfig.Message
            //        };
            //    }

            //    var message = "You are currently not allowed to place a bet. Please contact your agent for more information";
            //    if (request.Selections.Count() > 1)
            //    {
            //        if (!userBetConfig.Result.IsParlayEnabled)
            //        {
            //            return new BaseResponseDTO<BetResponseDTO>()
            //            {
            //                StatusCode = StatusCode.Not_Authorized,
            //                Message = message
            //            };
            //        }
            //    }
            //    else
            //    {
            //        if (!userBetConfig.Result.IsCricketEnabled)
            //        {
            //            if (request.Selections.Any(x => x.SportId == 21))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }
            //        if (!userBetConfig.Result.IsSoccerEnabled)
            //        {
            //            if (request.Selections.Any(x => x.SportId == 1))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }

            //        if (!userBetConfig.Result.IsSportsEnabled)
            //        {
            //            var excludedSports = new List<int>() { 21, 1 };
            //            if (request.Selections.Any(x => excludedSports.Contains(x.SportId)))
            //            {
            //                return new BaseResponseDTO<BetResponseDTO>()
            //                {
            //                    StatusCode = StatusCode.Not_Authorized,
            //                    Message = message
            //                };
            //            }
            //        }
            //    }
            //}
            #endregion
            decimal minStake = 0;
            decimal maxStake = 0;
            decimal maxPerGame = 0;
            decimal stakeFixtureLimit = 0;
            decimal parlayBonus = 0;
            var odds = request.IsBetBuilder ? request.Odds : request.Selections.Select(s => s.DecimalOdds).Aggregate((a, b) => a * b);
            var taxConfig = !string.IsNullOrEmpty(betConfig.TaxConfig) ? JsonSerializer.Deserialize<TaxConfig>(betConfig.TaxConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new TaxConfig();
            var priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();
            var betBuilderConfig = !string.IsNullOrEmpty(betConfig.BetBuilderConfig) ? JsonSerializer.Deserialize<BetBuilderConfig>(betConfig.BetBuilderConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new BetBuilderConfig();

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
                        StatusCode = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                //var stakeFixtureLimitResult = _bettingContext.StakeLimitFixture.Find($"{request.ClientCode}-{request.UserId}", request.Selections.First().FixtureId);

                //if (stakeFixtureLimitResult == null)
                //{
                //    if (stakeFixtureLimit > maxStake)
                //        stakeFixtureLimit = maxStake;
                //}
            }
            else
            {

                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                if (request.IsBetBuilder)
                {
                    minStake = GetMinStake(betBuilderConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForBetBuilderWithMTS(betBuilderConfig.MaxStake, mtsMaxStake.Result.Stake, request.CompanyShare);
                }
                else
                {
                    minStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeForParlayBetWithMTS(parlayConfig.MaxStake, mtsMaxStake.Result.Stake, parlayConfig.MaxPayout, odds, request.CompanyShare);
                }

                if (minStake > request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake,
                        Message = StatusCode.Stake_Must_Be_Higher_Or_Equal_To_Min_Stake.ToString().Replace('_', ' ')

                    };
                }

                if (maxStake < request.Stake)
                {
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake,
                        Message = StatusCode.Stake_Must_Be_Lower_Or_Equal_To_Max_Stake.ToString().Replace('_', ' ') + " " + Convert.ToDecimal(maxStake).ToString("#,##0.00"),
                        Result = new BetResponseDTO
                        {
                            Stake = Convert.ToDecimal(maxStake)
                        }
                    };
                }

                var hasVirtualSport = request.Selections.Any(s => s.SportId > 1000) ? true : false;

                if (request.IsBetBuilder == false && hasVirtualSport == false && request.Selections.Count >= 3 && parlayConfig.ParlayBonusConfig != null)
                {
                    var selectionCount = request.Selections.Where(c => c.DecimalOdds >= parlayConfig.ParlayBonusMinOdds).Count();
                    if (selectionCount >= 3)
                    {
                        var parlayBonusConfig = parlayConfig.ParlayBonusConfig.FirstOrDefault(c => c.SelectionCount == selectionCount);

                        if (parlayBonusConfig != null)
                            parlayBonus = parlayBonusConfig.ParlayBonus;
                    }
                }
            }

            var betData = new Bet();
            var betSelectionData = new List<BetSelectionMessageExtenstion>();

            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    Guid betId;

                    var totalOdds = request.IsBetBuilder ? request.Odds : request.Selections.Select(s => s.DecimalOdds).Aggregate((a, b) => a * b);
                    var totalOddsRef = request.Selections.Select(s => s.OddsRef).Aggregate((a, b) => a * b);
                    var selectionSports = request.Selections.Select(s => s.SportId).Distinct().ToList();

                    var stakeTax = taxConfig.StakeTax;

                    if (stakeTax > 0)
                    {
                        var excludedTaxedSport = !string.IsNullOrEmpty(taxConfig.ExcludeStakeSportTypes) ? taxConfig.ExcludeStakeSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedTaxedSport.Count == selectionSports.Count)
                            stakeTax = 0m;
                    }

                    var taxedStake = 0m; // request.Stake - (request.Stake / (1 + stakeTax));
                    var finalStake = request.Stake - taxedStake;
                    var potentialPayout = finalStake * totalOdds;

                    // Malay and Indo
                    if (request.Selections.Count == 1)
                    {
                        var selection = request.Selections.FirstOrDefault();

                        // malay code here
                        if ((int)request.OddsType == 1 && selection.MalaysianOdds < 0 && selection.IsBinary == true)
                        {
                            var currentStake = finalStake;
                            finalStake = Math.Abs(finalStake * Math.Round(selection.MalaysianOdds, 2));
                            potentialPayout = currentStake + finalStake;
                        }

                        // indo code here
                        if ((int)request.OddsType == 3 && selection.IndonesianOdds < 0 && selection.IsBinary == true)
                        {
                            var currentStake = finalStake;
                            finalStake = Math.Abs(finalStake * Math.Round(selection.IndonesianOdds, 2));
                            potentialPayout = currentStake + finalStake;
                        }

                        // hk code here
                    }

                    var winningsTax = taxConfig.WinningsTax;

                    if (winningsTax > 0)
                    {
                        var excludedWinningSport = !string.IsNullOrEmpty(taxConfig.ExcludeWinningsSportTypes) ? taxConfig.ExcludeWinningsSportTypes.Split(',').Select(int.Parse).Where(c => selectionSports.Contains(c)).ToList() : new List<int>();

                        if (excludedWinningSport.Count == selectionSports.Count)
                            winningsTax = 0m;
                    }

                    var betType = GetBetType(request.Selections, request.IsBoosted, request.IsBetBuilder);

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = finalStake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = potentialPayout,
                        WinLoss = 0m,
                        BetStatusId = (int)BetStatus.Waiting,
                        BetStatusName = BetStatus.Waiting.ToString(),
                        Reason = string.Empty,
                        CurrencyCode = request.CurrencyCode.ToString(),
                        LanguageCode = request.LanguageCode.ToString(),
                        ExchangeRate = request.ExchangeRate,
                        IPAddress = request.IsV4 ? request.IPAddressV4 : request.IPAddressV6,
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
                        IsFreeBet = false,
                        BetTypeId = (int)betType,
                        BetTypeName = betType.ToString(),
                        OddsRef = totalOddsRef,
                        IsAgent = request.IsAgent
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
                        IsFreeBet = betEntity.IsFreeBet,
                        BetTypeId = betEntity.BetTypeId,
                        BetTypeName = betEntity.BetTypeName,
                        OddsRef = betEntity.OddsRef,
                        IsAgent = betEntity.IsAgent
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
                            Odds = selection.DecimalOdds,
                            OddsRef = selection.OddsRef,
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
                            DeadHeatFactor = 1m,
                            IsBinary = selection.IsBinary
                        }).Entity;

                        betSelectionData.Add(new BetSelectionMessageExtenstion()
                        {
                            BetSelection = betSelectionEntity,
                            MarketSelectionCoumt = selection.MarketSelectionCount
                        });

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
                            OddsRef = betSelectionEntity.OddsRef,
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
                            DeadHeatFactor = betSelectionEntity.DeadHeatFactor,
                            IsBinary = betSelectionEntity.IsBinary
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

                    betData.IPAddress = request.IPAddressV4;

                    var payload = new OnBetPlacement
                    {
                        Bet = betData,
                        BetSelections = betSelectionData,
                        IsBoosted = request.IsBoosted,
                        IsBetBuilder = request.IsBetBuilder,
                        BonusId = request.BonusId,
                        Odds = request.Odds,
                        UserAgent = request.UserAgent,
                        IsOGClient = request.IsOGClient
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
                    Odds = betSelection.DecimalOdds
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

            var mtsMaxStake = await _mtsMaxStakeRepository.GetMTSMaxStake(mtsAuthToken.Result.Token, mtsConfigRequest, request.CompanyShare, request.IsBetBuilder, request.Odds);

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
            var odds = request.Selections.Select(s => s.DecimalOdds).Aggregate((a, b) => a * b);
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
            try
            {
                request.Page = request.Page <= 0 ? 1 : request.Page;
                request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;
                request.PageSize = request.PageSize > 50 ? 50 : request.PageSize;

                var skip = (request.Page - 1) * request.PageSize;
                double rowCount = 0;
                decimal winLoss = 0m;
                var result = new List<BetHistoryDTO>();
                var transactions = new List<Bet>();

                IQueryable<Bet> raw = null;

                var betTypeFilter = Enum.GetValues(typeof(BetType)).Cast<int>().ToList();
                if (request.BetType.Any())
                    betTypeFilter = request.BetType.ToList();

                if (request.IsCurrent)
                {
                    var openBets = new List<int> { (int)BetStatus.Running };

                    if (!string.IsNullOrEmpty(request.BetId))
                    {
                        if (Guid.TryParse(request.BetId, out var betId))
                            raw = _bettingContext.Bet.AsNoTracking().Where(c => c.BetId == betId && c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId) && betTypeFilter.Contains(c.BetTypeId));
                    }
                    else
                        raw = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId) && betTypeFilter.Contains(c.BetTypeId));

                    rowCount = transactions?.Count() ?? 0;
                    transactions = raw?.ToList() ?? new List<Bet>();
                }
                else
                {

                    if ((request.ToDate - request.FromDate).TotalDays > 30)
                        request.FromDate = request.ToDate.AddDays(-30);


                    var betStatuses = Enum.GetValues(typeof(BetStatus)).Cast<int>().ToList();
                    var betStatusIdsFilter = request.BetStatusFilter switch
                    {
                        BetStatusFilter.All => betStatuses.Where(c => !(new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Rejected }).Contains(c)),
                        BetStatusFilter.Open => betStatuses.Where(c => (new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running }).Contains(c)),
                        BetStatusFilter.Settled => betStatuses.Where(c => !(new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running, (int)BetStatus.Rejected }).Contains(c)),
                        _ => null
                    };

                    if (request.BetStatusFilter != BetStatusFilter.Open && request.BetStatus.Any())
                        betStatusIdsFilter = request.BetStatus.ToList();

                    if (!string.IsNullOrEmpty(request.BetId))
                    {
                        if (Guid.TryParse(request.BetId, out var betId))
                            raw = _bettingContext.Bet.AsNoTracking().Where(c => c.BetId == betId && c.UserId == request.UserId && c.ClientId == request.ClientId && betStatusIdsFilter.Contains(c.BetStatusId) && betTypeFilter.Contains(c.BetTypeId));
                    }
                    else
                        raw = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && betStatusIdsFilter.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate && betTypeFilter.Contains(c.BetTypeId));

                    rowCount = raw?.Count() ?? 0;
                    transactions = raw?.OrderByDescending(o => o.BetDate).Skip(skip).Take(request.PageSize).ToList() ?? new List<Bet>();
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

                var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                    && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();


                var runningBets = transactions.Where(c => c.BetStatusId == (int)BetStatus.Running).ToList();
                var runningBetIds = runningBets.Select(s => s.BetId).Distinct().ToList();
                var runningSelections = selections.Where(c => runningBetIds.Contains(c.BetId)).ToList();

                var partialCashoutAudit = _bettingContext.PartialCashoutAudit.AsNoTracking().Where(c => betIds.Contains(c.BetId)).OrderByDescending(o => o.CashoutDate).ToList();

                var cashOutList = await _sportsbookRepository.GetCashOut(runningBets, runningSelections, betBoosted);


                foreach (var bet in transactions)
                {
                    var cashOut = cashOutList.Result.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                    var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
                    var isBoosted = betBoosted.Any(b => b == bet.BetId);
                    var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

                    var hasVirtualSport = filteredSelections.Any(s => s.SportId > 1000) ? true : false;
                    var wonOrRunningSelectionCount = filteredSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Won || c.BetSelectionStatusId == (int)BetStatus.Running).Count();
                    var lostSelectionCount = filteredSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Lost).Count();

                    var parlayBonusWinnings = 0m;
                    var parlayBonus = 0m;
                    if (isBetBuilder == false && hasVirtualSport == false && wonOrRunningSelectionCount >= 3 && bet.IsFreeBet == false && lostSelectionCount == 0)
                    {
                        var parlayConfig = GetParlayBetConfig(_bettingContext, Guid.Parse(bet.ClientId), bet.CurrencyCode, bet.BetDate, filteredSelections, bet.Payout, ref parlayBonusWinnings);

                        if (parlayConfig.Item1 == true)
                        {
                            parlayBonus = parlayConfig.Item2.ParlayBonus;
                        }
                    }

                    var trueStake = bet.TrueStake;
                    var stake = bet.Stake;

                    var cashoutBets = bet.IsPartialCashout ? partialCashoutAudit.Where(c => c.BetId == bet.BetId) : new List<PartialCashoutAudit>();

                    var runningfinalStake = trueStake - bet.StakeBonus;
                    var runningTaxedStake = bet.IsFreeBet == false ? runningfinalStake - (runningfinalStake / (1 + bet.StakeTax)) : 0;
                    var runningStake = bet.BetStatusId == (int)BetStatus.Running ? GetStake(filteredSelections, runningTaxedStake, trueStake) : 0;
                    var runningPayout = bet.BetStatusId == (int)BetStatus.Running ? GetPayout(filteredSelections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), runningStake, stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId) : 0;
                    var runningTaxedWinnings = runningPayout + parlayBonusWinnings;

                    if (bet.IsFreeBet)
                        runningfinalStake = trueStake;

                    runningTaxedWinnings = bet.IsFreeBet == false && bet.StakeBonus == trueStake ? 0 : (runningTaxedWinnings - runningfinalStake) * bet.WinningsTax;

                    if (runningPayout < trueStake)
                        runningTaxedWinnings = 0;

                    if (bet.BetTypeId == (int)BetType.Parlay)
                    {
                        //recompute bet odds for parlay bets
                        bet.Odds = bet.BetStatusId == (int)BetStatus.Running ? bet.Odds : RecomputeBetOdds(bet.Odds, (BetStatus)bet.BetStatusId, filteredSelections);
                    }

                    result.Add(new BetHistoryDTO
                    {
                        BetId = bet.BetId.ToString(),
                        Stake = bet.Stake,
                        Odds = bet.Odds.ToString(),
                        Payout = bet.BetStatusId == (int)BetStatus.Running ? runningPayout : bet.Payout,
                        WinLoss = bet.WinLoss,
                        BetStatusId = bet.BetStatusId,
                        BetStatusName = bet.BetStatusName,
                        TransactionDate = bet.BetDate,
                        Commission = 0,
                        CurrencyCode = bet.CurrencyCode,
                        OddsTypeId = bet.OddsTypeId,
                        OddsType = bet.OddsTypeName,
                        CashOutStake = bet.BetStatusId == (int)BetStatus.Running ? cashOut.CashOutStake : 0,
                        CashOutStatusId = bet.BetStatusId == (int)BetStatus.Running ? (int)cashOut.CashOutStatus : (int)CashOutStatus.Unavailable,
                        CashOutStatusName = bet.BetStatusId == (int)BetStatus.Running ? cashOut.CashOutStatus.ToString() : CashOutStatus.Unavailable.ToString(),
                        StakeTax = bet.StakeTax,
                        WinningsTax = bet.WinningsTax,
                        TaxedStake = bet.BetStatusId == (int)BetStatus.Running ? runningTaxedStake : bet.TaxedStake,
                        TaxedWinnings = bet.BetStatusId == (int)BetStatus.Running ? runningTaxedWinnings : bet.TaxedWinnings,
                        ParlayBonus = parlayBonus,
                        ParlayBonusWinnings = bet.BetStatusId == (int)BetStatus.Running ? parlayBonusWinnings : bet.ParlayBonusWinnings,
                        StakeReal = bet.StakeReal,
                        StakeRealPayout = bet.StakeRealPayout,
                        StakeBonus = bet.StakeBonus,
                        StakeBonusPayout = bet.StakeBonusPayout,
                        IsFreeBet = bet.IsFreeBet,
                        IsBoosted = isBoosted,
                        IsBetBuilder = isBetBuilder,
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
                            Odds = GetSelectionOdds(bet.OddsTypeId, s.Odds, request.IsOddsString, s.IsBinary),
                            OddsTypeId = s.IsBinary ? bet.OddsTypeId : (int)OddsType.Decimal,
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
                            HalfLostProbability = bet.BetStatusId == (int)BetStatus.Running ? cashOut.Probabilities == null ? 0 : cashOut.Probabilities.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfLostProbability).FirstOrDefault() : filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfLostProbability).FirstOrDefault(),
                            DeadHeatFactor = s.DeadHeatFactor
                        }).ToList(),
                        PartialCashout = cashoutBets.Any() ? cashoutBets.Select(s => new PartialCashoutDTO
                        {
                            TransactionId = s.Id.ToString().ToUpper(),
                            Stake = s.StakeUsed,
                            Payout = s.CashoutAmount,
                            CashoutPercent = s.CashoutPercent,
                            TransactionDate = s.CashoutDate
                        }).ToList() : new List<PartialCashoutDTO>()
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
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<BaseResponseDTO<SettlementHistoryResponseDTO>> GetBetHistorySettled(BetHistorySettledRequestDTO request)
        {
            request.Page = request.Page <= 0 ? 1 : request.Page;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            request.PageSize = request.PageSize > 50 ? 50 : request.PageSize;

            var skip = (request.Page - 1) * request.PageSize;
            double rowCount = 0;
            var result = new List<SettlementHistoryDTO>();
            var transactions = new List<Bet>();

            var betStatusIds = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running, (int)BetStatus.Rejected };

            IQueryable<Bet> rawSettled;

            if (string.IsNullOrEmpty(request.UserId))
                rawSettled = _bettingContext.Bet.AsNoTracking().Where(c => c.ClientId == request.ClientId && !betStatusIds.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate);
            else
                rawSettled = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && !betStatusIds.Contains(c.BetStatusId) && c.BetDate >= request.FromDate && c.BetDate <= request.ToDate);

            rowCount = rawSettled.Count();
            transactions = rawSettled.OrderBy(o => o.BetDate).Skip(skip).Take(request.PageSize).ToList();

            if (transactions.Count == 0)
            {
                return new BaseResponseDTO<SettlementHistoryResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new SettlementHistoryResponseDTO
                    {
                        CurrentPage = 1,
                        PageCount = 1,
                        PageSize = 1,
                        RowCount = 0,
                        Bets = new List<SettlementHistoryDTO>()
                    }
                };
            }

            var betIds = transactions.Select(s => s.BetId).Distinct().ToList();
            var selections = _bettingContext.BetSelection.AsNoTracking().Where(c => betIds.Contains(c.BetId)).ToList();

            var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();
            var partialCashoutAudit = _bettingContext.PartialCashoutAudit.AsNoTracking().Where(c => betIds.Contains(c.BetId)).ToList();

            foreach (var bet in transactions)
            {
                var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
                var isBoosted = betBoosted.Any(b => b == bet.BetId);
                var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;
                var cashoutBets = bet.IsPartialCashout ? partialCashoutAudit.Where(c => c.BetId == bet.BetId) : new List<PartialCashoutAudit>();

                result.Add(new SettlementHistoryDTO
                {
                    BetId = bet.BetId.ToString(),
                    UserId = bet.UserId,
                    Stake = bet.Stake,
                    Odds = bet.Odds.ToString(),
                    Payout = bet.Payout,
                    WinLoss = bet.WinLoss,
                    BetStatusId = bet.BetStatusId,
                    BetStatusName = bet.BetStatusName,
                    TransactionDate = bet.BetDate,
                    SettlementDate = bet.SettlementDate,
                    Commission = 0,
                    CurrencyCode = bet.CurrencyCode,
                    OddsType = bet.OddsTypeName,
                    CashOutStake = 0m,
                    CashOutStatusId = (int)CashOutStatus.Unavailable,
                    CashOutStatusName = CashOutStatus.Unavailable.ToString(),
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
                    IsBetBuilder = isBetBuilder,
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
                        Odds = GetSelectionOdds(bet.OddsTypeId, s.Odds, request.IsOddsString, s.IsBinary),
                        OddsTypeId = s.IsBinary ? bet.OddsTypeId : (int)OddsType.Decimal,
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
                        Probability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.Probability).FirstOrDefault(),
                        WonProbability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.WonProbability).FirstOrDefault(),
                        LostProbability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.LostProbability).FirstOrDefault(),
                        VoidProbability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.VoidProbability).FirstOrDefault(),
                        HalfWonProbability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfWonProbability).FirstOrDefault(),
                        HalfLostProbability = filteredSelections.Where(c => c.BetSelectionId == s.BetSelectionId).Select(ss => ss.HalfLostProbability).FirstOrDefault(),
                        DeadHeatFactor = s.DeadHeatFactor
                    }).ToList(),
                    PartialCashout = cashoutBets.Any() ? cashoutBets.Select(s => new PartialCashoutDTO
                    {
                        TransactionId = s.Id.ToString().ToUpper(),
                        Stake = s.StakeUsed,
                        Payout = s.CashoutAmount,
                        CashoutPercent = s.CashoutPercent,
                        TransactionDate = s.CashoutDate
                    }).ToList() : new List<PartialCashoutDTO>()
                });
            }

            return new BaseResponseDTO<SettlementHistoryResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new SettlementHistoryResponseDTO
                {
                    CurrentPage = request.Page,
                    PageCount = (int)Math.Ceiling(rowCount / request.PageSize),
                    PageSize = request.PageSize,
                    RowCount = Convert.ToInt32(rowCount),
                    Bets = result
                }
            };
        }
        public async Task<BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>> GetBetHistoryRunning(BetHistoryRunningRequestDTO request)
        {
            var result = new List<BetHistoryRunning>();
            var transactions = new List<Bet>();

            var openBets = new List<int> { (int)BetStatus.Running };
            var openBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && openBets.Contains(c.BetStatusId)).ToList();

            if (openBetsResponse.Count > 0)
                transactions.AddRange(openBetsResponse);


            if (transactions.Count == 0)
            {
                return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetHistoryRunningFilteredResponseDTO()
                };
            }

            var betIds = transactions.Select(s => s.BetId).Distinct().ToList();
            var selections = _bettingContext.BetSelection.AsNoTracking().Where(c => betIds.Contains(c.BetId)).ToList();

            var betBoosted = _bettingContext.BetPriceBoost.AsNoTracking().Where(b => betIds.Contains(b.BetId) && b.BetStatusId != (int)BetStatus.Rejected
                                                && b.BetStatusId != (int)BetStatus.Cancelled).Select(s => s.BetId).ToList();


            var runningBets = transactions.Where(c => c.BetStatusId == (int)BetStatus.Running).ToList();
            var runningBetIds = runningBets.Select(s => s.BetId).Distinct().ToList();
            var runningSelections = selections.Where(c => runningBetIds.Contains(c.BetId)).ToList();

            var partialCashoutAudit = _bettingContext.PartialCashoutAudit.AsNoTracking().Where(c => betIds.Contains(c.BetId)).OrderByDescending(o => o.CashoutDate).ToList();

            var cashOutList = await _sportsbookRepository.GetCashOut(runningBets, runningSelections, betBoosted);

            foreach (var bet in transactions)
            {
                var cashOut = cashOutList.Result.Where(c => c.BetId == bet.BetId).FirstOrDefault();
                var filteredSelections = selections.Where(c => c.BetId == bet.BetId).ToList();
                var isBoosted = betBoosted.Any(b => b == bet.BetId);
                var isBetBuilder = bet.BetTypeId == (int)BetType.BetBuilder ? true : false;

                var hasVirtualSport = filteredSelections.Any(s => s.SportId > 1000) ? true : false;
                var wonOrRunningSelectionCount = filteredSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Won || c.BetSelectionStatusId == (int)BetStatus.Running).Count();
                var lostSelectionCount = filteredSelections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Lost).Count();

                var trueStake = bet.TrueStake;
                var stake = bet.Stake;

                var cashoutBets = bet.IsPartialCashout ? partialCashoutAudit.Where(c => c.BetId == bet.BetId) : new List<PartialCashoutAudit>();

                var runningfinalStake = trueStake - bet.StakeBonus;
                var runningTaxedStake = bet.IsFreeBet == false ? runningfinalStake - (runningfinalStake / (1 + bet.StakeTax)) : 0;
                var runningStake = GetStake(filteredSelections, runningTaxedStake, trueStake);
                var runningPayout = GetPayout(filteredSelections.Select(s => new Tuple<BetStatus, decimal, decimal, bool>((BetStatus)s.BetSelectionStatusId, s.Odds, s.DeadHeatFactor, s.IsBinary)).ToList(), runningStake, stake, isBetBuilder, bet.Odds, (OddsType)bet.OddsTypeId);

                var parlayBonusWinnings = 0m;
                var parlayBonus = 0m;
                if (isBetBuilder == false && hasVirtualSport == false && wonOrRunningSelectionCount >= 3 && bet.IsFreeBet == false && lostSelectionCount == 0)
                {
                    var parlayConfig = GetParlayBetConfig(_bettingContext, Guid.Parse(bet.ClientId), bet.CurrencyCode, bet.BetDate, filteredSelections, runningPayout, ref parlayBonusWinnings);

                    if (parlayConfig.Item1 == true)
                    {
                        parlayBonus = parlayConfig.Item2.ParlayBonus;
                    }
                }

                var runningTaxedWinnings = runningPayout + parlayBonusWinnings;

                if (bet.IsFreeBet)
                    runningfinalStake = trueStake;

                runningTaxedWinnings = bet.IsFreeBet == false && bet.StakeBonus == trueStake ? 0 : (runningTaxedWinnings - runningfinalStake) * bet.WinningsTax;

                if (runningPayout > trueStake)
                    runningPayout = runningPayout - runningTaxedWinnings;
                else
                    runningTaxedWinnings = 0;

                runningPayout = runningPayout + parlayBonusWinnings;

                result.Add(new BetHistoryRunning
                {
                    BetId = bet.BetId.ToString(),
                    CashOutStake = bet.BetStatusId == (int)BetStatus.Running ? cashOut.CashOutStake : 0,
                    CashOutStatusId = bet.BetStatusId == (int)BetStatus.Running ? (int)cashOut.CashOutStatus : (int)CashOutStatus.Unavailable,
                    CashOutStatusName = bet.BetStatusId == (int)BetStatus.Running ? cashOut.CashOutStatus.ToString() : CashOutStatus.Unavailable.ToString(),
                    TaxedWinnings = runningTaxedWinnings,
                    TaxedStake = runningTaxedStake,
                    Payout = runningPayout,
                    PartialCashout = cashoutBets.Any() ? cashoutBets.Select(s => new PartialCashoutDTO
                    {
                        TransactionId = s.Id.ToString().ToUpper(),
                        Stake = s.StakeUsed,
                        Payout = s.CashoutAmount,
                        CashoutPercent = s.CashoutPercent,
                        TransactionDate = s.CashoutDate
                    }).ToList() : new List<PartialCashoutDTO>()
                });
            }

            return new BaseResponseDTO<BetHistoryRunningFilteredResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetHistoryRunningFilteredResponseDTO
                {
                    Bets = result
                }
            };
        }
        public async Task<BaseResponseDTO<BetConfigFEResponseDTO>> GetBetConfig(BetConfigRequestDTO request)
        {
            try
            {
                var result = new BetConfigFEResponseDTO();
                var clientId = new Guid(request.ClientId);
                var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == clientId && x.CurrencyCode == request.CurrencyCode).FirstOrDefault();

                DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                TaxConfig taxConfig = !string.IsNullOrEmpty(betConfig.TaxConfig) ? JsonSerializer.Deserialize<TaxConfig>(betConfig.TaxConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new TaxConfig();
                PriceboostConfig priceboostConfig = !string.IsNullOrEmpty(betConfig.PriceboostConfig) ? JsonSerializer.Deserialize<PriceboostConfig>(betConfig.PriceboostConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new PriceboostConfig();
                BetBuilderConfig betBuilderConfig = !string.IsNullOrEmpty(betConfig.BetBuilderConfig) ? JsonSerializer.Deserialize<BetBuilderConfig>(betConfig.BetBuilderConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : new BetBuilderConfig();

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
                    WinningsTax = taxConfig.WinningsTax,
                    ExcludeStakeSportTypes = taxConfig.ExcludeStakeSportTypes,
                    ExcludeWinningsSportTypes = taxConfig.ExcludeWinningsSportTypes
                };

                result.PriceboostConfig = priceboostConfig;
                result.BetBuilderConfig = betBuilderConfig;

                return new BaseResponseDTO<BetConfigFEResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<BaseResponseDTO<VerticalConfig>> GetUserBetConfig(UserBetConfigDetailsRequestDTO request)
        {
            try
            {
                var result = new VerticalConfig(true);
                var clientId = new Guid(request.ClientId);
                var userBetConfig = _bettingContext.UserBetConfig.FirstOrDefault(x => x.ClientId == clientId && x.UserId == request.UserId);

                if (userBetConfig != null)
                {
                    result = JsonSerializer.Deserialize<VerticalConfig>(userBetConfig.VerticalConfig);
                }

                return new BaseResponseDTO<VerticalConfig>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<VerticalConfig>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message,
                    Result = null
                };
            }
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

                        newBet.Selections = betSelections.Where(m => m.BetId == betId).Select(s => new BetSelectionDetailsDTO()
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
                bet.Selections = filteredSelections.Select(s => new BetSelectionDetailsDTO
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

        public async Task<BaseResponseDTO<BetSelectionDetailsResponseDTO>> GetBetSelections(BetSelectionDetailsRequestDTO request)
        {
            var selections = _bettingContext.BetSelection.AsNoTracking().Where(s => s.BetId == request.BetId).ToList();

            if (selections == null || !selections.Any())
            {
                return new BaseResponseDTO<BetSelectionDetailsResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetSelectionDetailsResponseDTO
                    {
                        Selections = new List<BetSelectionDetailsDTO>()
                    }
                };
            }

            var newSelections = new List<BetSelectionDetailsDTO>();

            foreach (var s in selections)
            {
                newSelections.Add(new BetSelectionDetailsDTO
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
                });
            }

            return new BaseResponseDTO<BetSelectionDetailsResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetSelectionDetailsResponseDTO
                {
                    Selections = newSelections
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

            if (bet.BetTypeId == (int)BetType.BetBuilder)
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

            var requestCashoutStakeFull = request.IsPartialCashout ? request.CashOutStake / request.CashOutStakePercent : request.CashOutStake;

            if (Math.Round(requestCashoutStakeFull, 2) != Math.Round(cashOut.CashOutStake, 2))
            {
                var requestCashOutStake = requestCashoutStakeFull % 1 == 0 ? requestCashoutStakeFull.ToString().TrimEnd('0').TrimEnd('.') : requestCashoutStakeFull.ToString().TrimEnd('0').TrimEnd('.');
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
                var correlationId = Guid.NewGuid().ToString().ToUpper();
                if (request.IsPartialCashout)
                {
                    var payload = new OnCashoutBetSettlement
                    {
                        BetId = request.BetId,
                        UserId = bet.UserId,
                        ClientCode = bet.ClientCode,
                        ClientId = bet.ClientId.ToUpper(),
                        CashoutType = CashoutType.Partial,
                        CashOutStake = request.CashOutStake,
                        CashOutStakePercent = request.CashOutStakePercent,
                        TaxedStake = cashOut.TaxedStake * request.CashOutStakePercent,
                        TaxedWinnings = cashOut.TaxedWinnings * request.CashOutStakePercent,
                        NonTaxedPayout = cashOut.NonTaxedPayout * request.CashOutStakePercent,
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
                        CorrelationId = correlationId,
                        MessageType = "OnBetPartialCashOut",
                        RoutingKey = "r.bet.cashout"
                    });
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
                        CashOutStakePercent = 1,
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
                        CorrelationId = correlationId,
                        MessageType = "OnBetFullCashOut",
                        RoutingKey = "r.bet.cashout"
                    });
                }


                return new BaseResponseDTO<BetCashOutResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetCashOutResponseDTO
                    {
                        BetId = bet.BetId.ToString().ToUpper(),
                        TransactionId = correlationId,
                        BetStatus = (BetStatus)bet.BetStatusId,
                        UserId = bet.UserId,
                        CashOutStake = request.CashOutStake
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

        public async Task<BaseResponseDTO<BetStatus>> GetBetStatus(BetStatusRequestDTO request)
        {
            try
            {
                var bet = _bettingContext.Bet.Find(request.BetId);

                return new BaseResponseDTO<BetStatus>
                {
                    StatusCode = StatusCode.Success,
                    Result = (BetStatus)Enum.Parse(typeof(BetStatus), bet.BetStatusName)
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetStatus>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }
        public async Task<BaseResponseDTO<BetRecommendationsResponseDTO>> GetBetRecommenations(BetRecommendationRequestDTO request)
        {
            var clientTopMarkets = _bettingContext.ClientTopMarket.AsNoTracking()
                                            .Where(c => c.ClientId == request.ClientId.ToString() && c.MarketStatusId == (int)MarketStatus.Active)
                                            .ToList();

            var markets = clientTopMarkets.OrderByDescending(o => o.BetCount)
                                            .Take(10)
                                            .Select((s, i) => new BetRecommendationMarketDTO()
                                            {
                                                FixtureId = s.FixtureId,
                                                MarketId = s.MarketId,
                                                SortOrder = i + 1
                                            })
                                            .ToList();

            return await _sportsbookRepository.GetBetRecommendationDetails(new BetRecommendationDetailRequestDTO()
            {
                ClientId = request.ClientId,
                ClientCode = request.ClientCode,
                Markets = markets
            });
        }

        public async Task<BaseResponseDTO<bool>> DeleteCachePreload(CachePreloadRequestDTO request)
        {
            if (request.CacheType == CacheType.ParlayBonus)
                _memCache.DeleteCache("pba");

            return new BaseResponseDTO<bool>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = true
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
        private decimal GetMaxStakeForBetBuilderWithMTS(decimal maxStakeBackOffice, decimal maxStakeMTS, decimal companyShare)
        {
            List<decimal> maximumStake = new List<decimal>();

            if (maxStakeMTS == 0)
                maximumStake.Add(maxStakeBackOffice);
            else
                maximumStake.Add(maxStakeMTS);

            return maximumStake.Min();
        }

        private Tuple<bool, ParlayBonusAudit> GetParlayBetConfig(BettingContext context, Guid clientId, string currencyCode, DateTime betDate, List<BetSelection> selections, decimal payout, ref decimal parlayBonusWinnings)
        {
            var parlayBonusList = GetParlayBonusAudit(context).Where(c => c.ClientId == clientId && c.TimeStamp < betDate
                    && c.CurrencyCode == currencyCode).OrderByDescending(o => o.TimeStamp).ToList();

            if (parlayBonusList == null || !parlayBonusList.Any())
                return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());

            int wonOrRunningSelectionCount = selections.Where(c => (c.BetSelectionStatusId == (int)BetStatus.Won || c.BetSelectionStatusId == (int)BetStatus.Running) && c.Odds >= parlayBonusList.FirstOrDefault().ParlayBonusMinOdds).Count();

            if (wonOrRunningSelectionCount >= 3)
            {
                var result = parlayBonusList.Where(c => wonOrRunningSelectionCount == c.SelectionCount).OrderByDescending(o => o.TimeStamp).FirstOrDefault();

                if (result == null)
                    return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                else
                {
                    parlayBonusWinnings = payout * result.ParlayBonus;

                    if (parlayBonusWinnings > result.ParlayBonusMaxWinnings && result.ParlayBonusMaxWinnings > 0)
                    {
                        parlayBonusWinnings = 0;
                        return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());
                    }
                    else
                        return new Tuple<bool, ParlayBonusAudit>(true, result);
                }
            }
            else
                return new Tuple<bool, ParlayBonusAudit>(false, new ParlayBonusAudit());

        }

        private List<ParlayBonusAudit> GetParlayBonusAudit(BettingContext context)
        {
            return _memCache.GetCache($"pba", () => context.ParlayBonusAudit.ToList(), 1);
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
            List<int> eSports = new List<int> { 109, 110, 111, 112, 125, 134, 137, 153, 195 };
            List<int> virtualSports = new List<int> { 1001, 1002, 1003, 1005, 1021, 1055, 1058 };

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

        private BetType GetBetType(List<BetSelectionRequestDTO> selections, bool isBoosted, bool isBetBuilder)
        {
            if (isBoosted)
                return BetType.PriceBoost;
            if (isBetBuilder)
                return BetType.BetBuilder;

            return selections.Count > 1 ? BetType.Parlay : BetType.Single;

        }

        private decimal GetStake(List<BetSelection> selections, decimal taxedStake, decimal trueStake)
        {
            var totalSelections = selections.Count();
            var cancelledSelections = selections.Where(c => c.BetSelectionStatusId == (int)BetStatus.Cancelled).ToList();

            if (cancelledSelections.Count == totalSelections || selections.Any(c => c.BetSelectionStatusId == (int)BetStatus.Lost))
                return trueStake;

            //apply for Won, HalfWon, HalfLost, Void
            return trueStake - taxedStake;
        }

        private decimal GetPayout(List<Tuple<BetStatus, decimal, decimal, bool>> selections, decimal trueStake, decimal stake, bool isBetBuilder, decimal odds, OddsType oddsType)
        {
            var values = new List<decimal>();

            if (selections.Count == 1 && OddsTypeHelper.GetOddsByOddsType((int)oddsType, selections.First().Item2, selections.First().Item4) < 0 && (oddsType == OddsType.Indonesian || oddsType == OddsType.Malaysian))
            {
                switch (selections.First().Item1)
                {
                    case BetStatus.Lost:
                        return 0m * trueStake;

                    case BetStatus.HalfWon:
                        return (stake / 2) + trueStake;

                    case BetStatus.HalfLost:
                        return 0.5m * trueStake;

                    case BetStatus.Won:
                    case BetStatus.Running:
                        return trueStake + stake;

                    case BetStatus.Void:
                        return 1m * trueStake;

                    case BetStatus.Cancelled:
                        return 1m * trueStake;
                }
            }

            if (isBetBuilder == true)
            {
                if (selections.Count() == selections.Where(s => s.Item1 == BetStatus.Won || s.Item1 == BetStatus.Running).Count())
                    return (odds * trueStake);
                else if (selections.Any(s => s.Item1 == BetStatus.Lost))
                    return 0m;
                else if (selections.Any(s => s.Item1 == BetStatus.Void))
                    return trueStake;
                else if (selections.Any(s => s.Item1 == BetStatus.Cancelled))
                    return trueStake;
                else
                    return trueStake;
            }

            foreach (var selection in selections)
            {
                switch (selection.Item1)
                {
                    case BetStatus.Lost:
                        values.Add(0m);
                        break;
                    case BetStatus.HalfWon:
                        values.Add((selection.Item2 + 1) / 2);
                        break;
                    case BetStatus.HalfLost:
                        values.Add(0.5m);
                        break;
                    case BetStatus.Running:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Won:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b) * trueStake;
        }

        private object GetSelectionOdds(int oddsTypeId, object odds, bool isOddsString, bool isBinary)
        {
            if (isOddsString == true)
                return OddsTypeHelper.GetOddsByOddsType(oddsTypeId, Convert.ToDecimal(odds.ToString()), isBinary).ToString("0.00");
            else
                return OddsTypeHelper.GetOddsByOddsType(oddsTypeId, Convert.ToDecimal(odds.ToString()), isBinary);
        }

        private decimal RecomputeBetOdds(decimal odds, BetStatus betStatus, List<BetSelection> betSelections)
        {
            decimal newOdds = 0m;
            List<decimal> values = new List<decimal>();

            switch (betStatus)
            {
                case BetStatus.Won:
                case BetStatus.HalfWon:
                case BetStatus.HalfLost:
                    foreach (var selection in betSelections)
                    {
                        switch ((BetStatus)selection.BetSelectionStatusId)
                        {
                            case BetStatus.Lost:
                                values.Add(0m);
                                break;
                            case BetStatus.HalfWon:
                                values.Add((selection.Odds + 1) / 2);
                                break;
                            case BetStatus.HalfLost:
                                values.Add(0.5m);
                                break;
                            case BetStatus.Won:
                            case BetStatus.Running:
                                values.Add(selection.Odds * selection.DeadHeatFactor);
                                break;
                            case BetStatus.Void:
                                values.Add(1m);
                                break;
                            case BetStatus.Cancelled:
                                values.Add(1m);
                                break;
                        }
                    }

                    newOdds = values.Aggregate((a, b) => a * b);

                    break;
                default:
                    return odds;
            }

            return newOdds;
        }

    }
}
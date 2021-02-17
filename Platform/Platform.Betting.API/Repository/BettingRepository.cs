using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
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
        private readonly IMessageBroker _messageBroker;
        private readonly IHttpClientFactory _httpClientFactory;
        public BettingRepository(BettingContext bettingContext, IMessageBroker messageBroker, IHttpClientFactory httpClientFactory)
        {
            _bettingContext = bettingContext;
            _httpClientFactory = httpClientFactory;
            _messageBroker = messageBroker;
            _messageBroker.Connect();
        }
        public async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequestDTO request)
        {
            //TODO: bet validation
            var clientId = new Guid(request.ClientId);
            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == clientId && x.CurrencyCode == request.CurrencyCode.ToString()).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
            decimal minStake = 0;
            decimal maxStake = 0;
            if (request.Selections.Count() == 1)
            {
                //Get MTS Information
                var mtsConfigSelectionRequest = new List<MTSConfigSelectionRequestDTO>();
                mtsConfigSelectionRequest.Add(new MTSConfigSelectionRequestDTO
                {
                    FixtureSpecifier = request.Selections.FirstOrDefault().FixtureSpecifier,
                    SelectionSpecifier = request.Selections.FirstOrDefault().SelectionSpecifier,
                    Odds = request.Selections.FirstOrDefault().Odds
                });

                var mtsConfigRequest = new MTSConfigRequestDTO
                {
                    UserId = request.UserId,
                    Currency = request.CurrencyCode.ToString(),
                    Language = request.LanguageCode.ToString(),
                    DeviceId = request.Channel.ToString(),
                    LimitId = int.Parse( request.LimitId),
                    BetSelections = mtsConfigSelectionRequest
                };

                //SingleBet
                var selection = request.Selections.FirstOrDefault();
                //Check first if sportsId exist in Override
                List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                var checkOverride = overrideSportConfig.Where(a=>a.SportId == selection.SportId).FirstOrDefault();
                if (checkOverride != null)
                {
                    minStake = GetMinStake(checkOverride.MinStake,request.ExchangeRate, request.CompanyShare,request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeSingleBet(checkOverride.MinStake, request.ExchangeRate,true, mtsConfigRequest,request);
                }
                else
                {
                    DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    minStake = GetMinStake(defaultSportConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                    maxStake = GetMaxStakeSingleBet(defaultSportConfig.MinStake, request.ExchangeRate, true, mtsConfigRequest, request);
                }

            }
            else
            {
                //Parlay
                //Get MTS Information
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
                    UserId = request.UserId,
                    Currency = request.CurrencyCode.ToString(),
                    Language = request.LanguageCode.ToString(),
                    DeviceId = request.Channel.ToString(),
                    LimitId = int.Parse(request.LimitId),
                    BetSelections = mtsConfigSelectionRequest
                };

                ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                minStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode.ToString());
                maxStake = GetMaxStakeParlay(parlayConfig.MinStake,parlayConfig.MaxPayout,request.CompanyShare,0,true,mtsConfigRequest, request);
            }

            //TODO: Max Fixture Bet 
            var betFixtureLimits = _bettingContext.StakeLimitFixture.Find(request.ClientId, request.Selections.FirstOrDefault().FixtureId);


            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                try
                {
                    Guid betId;

                    var totalOdds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
                    var stake = request.Stake;

                    var betEntity = _bettingContext.Bet.Update(new Bet
                    {
                        UserId = request.UserId,
                        Stake = request.Stake,
                        TrueStake = stake,
                        Odds = totalOdds,
                        OddsTypeId = (int)request.OddsType,
                        OddsTypeName = request.OddsType.ToString(),
                        Payout = stake == request.Stake ? stake * totalOdds : stake + request.Stake,
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
                        ClientCode = request.ClientCode,
                        LimitId = request.LimitId,
                        Endpoint = request.Endpoint,
                        OperatorToken = request.OperatorToken,
                        CompanyShare = request.CompanyShare,
                        IsResettled = false,
                        IsCashout = false,
                        BetDate = request.BetDate,
                        BetDateOffset = request.BetDate.AddHours(-4),
                        SettlementDate = request.BetDate,
                        SettlementDateOffset = request.BetDate.AddHours(-4)
                    }).Entity;

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
                        ClientCode = betEntity.ClientCode,
                        LimitId = betEntity.LimitId,
                        Endpoint = betEntity.Endpoint,
                        OperatorToken = betEntity.OperatorToken,
                        CompanyShare = betEntity.CompanyShare,
                        IsResettled = betEntity.IsResettled,
                        IsCashout = betEntity.IsCashout,
                        BetDate = betEntity.BetDate,
                        BetDateOffset = betEntity.BetDateOffset,
                        SettlementDate = betEntity.SettlementDate,
                        SettlementDateOffset = betEntity.SettlementDateOffset
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
                            BetHomeScore = selection.BetHomeScore,
                            BetAwayScore = selection.BetAwayScore,
                            SettlementHomeScore = string.Empty,
                            SettlementAwayScore = string.Empty,
                            CorrelationId = selection.CorrelationId
                        }).Entity;

                        var betSelectionId = betSelectionEntity.BetSelectionId;
                        selection.BetSelectionId = betSelectionId;

                        _bettingContext.BetSelectionAudit.Add(new BetSelectionAudit
                        {
                            BetId = betId,
                            BetSelectionId = betSelectionId,
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
                            SelectionTemplateId = selection.SelectionTemplateId,
                            SelectionId = selection.SelectionId,
                            SelectionName = selection.SelectionName,
                            FixtureStatusId = (int)selection.FixtureStatus,
                            FixtureStatusName = selection.FixtureStatus.ToString(),
                            Line = selection.Line,
                            Odds = selection.Odds,
                            BetSelectionStatusId = betEntity.BetStatusId,
                            BetSelectionStatusName = betEntity.BetStatusName,
                            IsSettled = false,
                            IsResettled = false,
                            FixtureSpecifier = selection.FixtureSpecifier,
                            SelectionSpecifier = selection.SelectionSpecifier,
                            FixtureStartDate = selection.FixtureStartDate,
                            BetHomeScore = selection.BetHomeScore,
                            BetAwayScore = selection.BetAwayScore,
                            SettlementHomeScore = string.Empty,
                            SettlementAwayScore = string.Empty,
                            CorrelationId = selection.CorrelationId
                        });
                    }

                    _bettingContext.SaveChanges();
                    scope.Complete();

                    _messageBroker.Publish(new OnBetPlacement { BetId = betId }, "OnBetPlacement", "r.bet.placement", Guid.NewGuid().ToString().ToUpper());
                }
                catch (Exception ex)
                {
                    scope.Dispose();
                    return new BaseResponseDTO<BetResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                    };
                }
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
                var openBets = new List<int> { (int)BetStatus.Waiting, (int)BetStatus.Running };
                var openBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => openBets.Contains(c.BetStatusId)).ToList();

                var cancelledRejectedBets = new List<int> { (int)BetStatus.Cancelled, (int)BetStatus.Rejected };
                var cancelledRejectedBetsResponse = _bettingContext.Bet.AsNoTracking().Where(c => openBets.Contains(c.BetStatusId) && c.BetDate >= DateTime.UtcNow.AddMinutes(5)).ToList();

                if (openBetsResponse.Count > 0)
                    transactions.AddRange(openBetsResponse);

                if (cancelledRejectedBetsResponse.Count > 0)
                    transactions.AddRange(cancelledRejectedBetsResponse);

                rowCount = Convert.ToDouble(transactions.Count());
            }
            else
            {
                if (request.BetStatusFilter == BetStatusFilter.All)
                {
                    var rawAll = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && c.BetDate.Date >= request.FromDate && c.BetDate.Date <= request.ToDate.Date);

                    rowCount = rawAll.Count();
                    winLoss = rawAll.Sum(c => c.WinLoss);
                    transactions = rawAll.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Open)
                {
                    var rawOpen = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date && c.BetStatusId < (int)BetStatus.Won);

                    rowCount = rawOpen.Count();
                    winLoss = rawOpen.Sum(c => c.WinLoss);
                    transactions = rawOpen.OrderByDescending(c => c.BetDateOffset).Skip(skip).Take(request.PageSize).ToList();
                }

                if (request.BetStatusFilter == BetStatusFilter.Settled)
                {
                    var rawSettled = _bettingContext.Bet.AsNoTracking().Where(c => c.UserId == request.UserId && c.ClientId == request.ClientId && c.BetDateOffset.Date >= request.FromDate.Date && c.BetDateOffset.Date <= request.ToDate.Date && c.BetStatusId > (int)BetStatus.Running);

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

            foreach (var bet in transactions)
            {
                var selections = _bettingContext.BetSelection.AsNoTracking().Where(c => c.BetId == bet.BetId);

                result.Add(new BetHistoryDTO
                {
                    BetId = bet.BetId.ToString(),
                    Stake = bet.Stake,
                    Odds = bet.Odds,
                    Payout = bet.Payout,
                    WinLoss = bet.WinLoss,
                    BetStatus = (BetStatus)bet.BetStatusId,
                    TransactionDate = bet.BetDate,
                    Commission = 0,
                    CurrencyCode = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), bet.CurrencyCode),
                    OddsType = (OddsType)bet.OddsTypeId,
                    Selections = selections.Select(s => new BetSelectionHistoryDTO
                    {
                        SportId = s.SportId,
                        SportName = s.SportName,
                        LeagueId = s.LeagueId,
                        LeagueName = s.LeagueName,
                        FixtureId = s.FixtureId,
                        FixtureName = s.FixtureName,
                        MarketId = s.MarketId,
                        MarketName = s.MarketName,
                        Line = s.Line,
                        SelectionId = s.SelectionId,
                        SelectionName = s.SelectionName,
                        Odds = s.Odds,
                        BetAwayScore = s.BetAwayScore,
                        BetHomeScore = s.BetHomeScore,
                        BetSelectionId = s.BetSelectionId.ToString(),
                        FixtureStartDate = s.FixtureStartDate,
                        FixtureStatus = (FixtureStatus)s.FixtureStatusId,
                        MarketTemplateId = s.MarketTemplateId,
                        SegmentId = s.SegmentId,
                        SegmentName = s.SegmentName,
                        SelectionTemplateId = s.SelectionTemplateId,
                        SettlementAwayScore = s.SettlementAwayScore,
                        SettlementHomeScore = s.SettlementHomeScore

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
                    Bets = result
                }
            };
        }

        public async Task<BaseResponseDTO<BetConfigResponseDTO>> GetBetConfig(BetConfigRequestDTO request)
        {
            var result = new BetConfigResponseDTO();
            var clientId = new Guid(request.ClientId);
            var betConfig = _bettingContext.BetConfig.Where(x => x.ClientId == clientId && x.CurrencyCode == request.CurrencyCode).OrderByDescending(x => x.CreatedDate).FirstOrDefault();

            DefaultSportConfig defaultSportConfig = JsonSerializer.Deserialize<DefaultSportConfig>(betConfig.DefaultConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            ParlayConfig parlayConfig = JsonSerializer.Deserialize<ParlayConfig>(betConfig.ParlayConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            List<OverrideSportConfig> overrideSportConfig = JsonSerializer.Deserialize<List<OverrideSportConfig>>(betConfig.OverrideConfig, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });


            result.DefaultSportConfig = new DefaultSportConfig()
            {
                MinStake = GetMinStake(defaultSportConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode),
                MaxStake = GetMaxStakeSingleBet(defaultSportConfig.MaxStake,request.CompanyShare,false, null,null),
                StakeLimitFixture = defaultSportConfig.StakeLimitFixture
            };

            result.OverrideSportConfig = new List<OverrideSportConfig>();
            foreach (var overRide in overrideSportConfig)
            {
                OverrideSportConfig newOverrideSport = new OverrideSportConfig();
                newOverrideSport.SportId = overRide.SportId;
                newOverrideSport.SportName = overRide.SportName;
                newOverrideSport.MinStake = GetMinStake(overRide.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode);
                newOverrideSport.MaxStake = GetMaxStakeSingleBet(overRide.MaxStake,request.CompanyShare,false,null,null);
                newOverrideSport.StakeLimitFixture = overRide.StakeLimitFixture;
                result.OverrideSportConfig.Add(newOverrideSport);
            }

            result.Parlayconfig = new ParlayConfig()
            {
                MinStake = GetMinStake(parlayConfig.MinStake, request.ExchangeRate, request.CompanyShare, request.CurrencyCode),
                MaxStake = GetMaxStakeParlay(parlayConfig.MaxStake, parlayConfig.MaxPayout, request.CompanyShare, 0, false,null,null),
                MaxPayout = parlayConfig.MaxPayout
            };

            return new BaseResponseDTO<BetConfigResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = result
            };
        }

        public decimal GetMinStake(decimal minStake, decimal exchangeRate, decimal companyShare, string currencyCode)
        {
            List<decimal> minimumStake = new List<decimal>();
            var highCurrencies = new List<string> { "KRW", "VND", "KHR", "IDR", "MMK" };

            //1. Reference Bet
            minimumStake.Add(Math.Truncate(exchangeRate) + 1);

            //2. High Currency add 1000 default
            if (highCurrencies.Contains(currencyCode))
                minimumStake.Add(1000m);

            //3 Get Min from betconfig
            minimumStake.Add(minStake);

            //4 Reference Bet / Company Share
            if (companyShare > 0)
                minimumStake.Add(Math.Truncate(exchangeRate / companyShare) + 1);

            //Get Max decimal value
            return minimumStake.Max();
        }

        public decimal GetMaxStakeParlay(decimal maxStake, decimal parlayMaxPayout, decimal companyShare, decimal odds, bool isPlacement, MTSConfigRequestDTO mtsConfigRequest, BetRequestDTO request)
        {
            List<decimal> maximumStake = new List<decimal>();

            //1 Get Max from betconfig
            maximumStake.Add(maxStake);
            maximumStake.Add(parlayMaxPayout);

            

            //BET PLACEMENT ONLY
            if (isPlacement)
            {
                //Max Payout/Total Odds
                var totalOdds = request.Selections.Select(s => s.Odds).Aggregate((a, b) => a * b);
                maximumStake.Add(Math.Truncate(parlayMaxPayout / totalOdds) + 1);

                //Get Max Liability by Tier from Backoffice database
                var tierMaxStake = GetTierMaxStake(new BetConfigLeagueRequestDTO()
                {
                    LeagueId = request.Selections.FirstOrDefault().LeagueId,
                    SegmentId = request.Selections.FirstOrDefault().SegmentId,
                    SportId = request.Selections.FirstOrDefault().SportId
                });

                if (tierMaxStake.Result != null)
                {
                    maximumStake.Add(Math.Truncate(tierMaxStake.Result.MaxStake / (totalOdds - 1)) + 1);
                }

                //Get MTS maxstake
                //(**) For Max Bet: If Company Share > 0%, do not consider MTS Max Stake value from Max Stake API in Max bet logic (i.e. whichever is lowest).  
                //Difference from [MTS Max Stake/Company Share] is required.
                var mtsMaxStake = GetMTSMaxStake(mtsConfigRequest);
                if (mtsMaxStake.Result > 0)
                    //MTS Max Stake from Max Stake API
                    maximumStake.Add(Math.Truncate(mtsMaxStake.Result / companyShare) + 1);

            }

            //Get min decimal value
            return maximumStake.Min();
        }
        public decimal GetMaxStakeSingleBet(decimal maxStake, decimal companyShare, bool isPlacement, MTSConfigRequestDTO mtsConfigRequest, BetRequestDTO request)
        {
            List<decimal> maximumStake = new List<decimal>();

            //1 Get Max from betconfig
            maximumStake.Add(maxStake);

            //BET PLACEMENT ONLY
            if (isPlacement)
            {
                var totalOdds = request.Selections.FirstOrDefault().Odds;

                //Get Max Liability by Tier from Backoffice database
                var tierMaxStake = GetTierMaxStake(new BetConfigLeagueRequestDTO()
                {
                    LeagueId = request.Selections.FirstOrDefault().LeagueId,
                    SegmentId = request.Selections.FirstOrDefault().SegmentId,
                    SportId = request.Selections.FirstOrDefault().SportId
                });

                //BO max Liability/Total Odds
                if (tierMaxStake.Result != null)
                {
                    maximumStake.Add(Math.Truncate(tierMaxStake.Result.MaxStake / (totalOdds - 1)) + 1);
                }

                //Get MTS maxstake
                //(**) For Max Bet: If Company Share > 0%, do not consider MTS Max Stake value from Max Stake API in Max bet logic (i.e. whichever is lowest).  
                //Difference from [MTS Max Stake/Company Share] is required.
                var mtsMaxStake = GetMTSMaxStake(mtsConfigRequest);
                if (companyShare == 0)
                {
                    if (mtsMaxStake.Result > 0)
                        //MTS Max Stake from Max Stake API
                        maximumStake.Add(mtsMaxStake.Result);
                }
                else
                {
                    if (mtsMaxStake.Result > 0)
                        //MTS Max Stake from Max Stake API
                        maximumStake.Add(Math.Truncate(mtsMaxStake.Result / companyShare) + 1);
                }
                
            }

            //Get min decimal value
            return maximumStake.Min();
        }

        public BaseResponseDTO<decimal> GetMTSMaxStake(MTSConfigRequestDTO request)
        {
            var client = _httpClientFactory.CreateClient("betting-song88");

            var response = client.PostAsJsonAsync($"config/mts", request).Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BaseResponseDTO<decimal>>(response.Content.ReadAsStringAsync().Result);
            }

            return new BaseResponseDTO<decimal>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = 0m
            };
        }

        public BaseResponseDTO<BetConfigLeagueResponse> GetTierMaxStake(BetConfigLeagueRequestDTO request)
        {
            var client = _httpClientFactory.CreateClient("betting-song88");

            var response = client.PostAsJsonAsync($"tier/maxstake", request).Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BaseResponseDTO<BetConfigLeagueResponse>>(response.Content.ReadAsStringAsync().Result);
            }

            return new BaseResponseDTO<BetConfigLeagueResponse>()
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetConfigLeagueResponse()
            };
        }
    }
}

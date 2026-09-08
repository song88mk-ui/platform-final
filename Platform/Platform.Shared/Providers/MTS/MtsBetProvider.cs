using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Platform.Shared.Repositories;
using Sportradar.MTS.SDK.Entities.Builders;
using Platform.Shared.MessageModels;
using Platform.Shared.Extensions;
using System.Linq;
using System.Threading;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.Interfaces;
using System.Net;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Platform.Shared.Providers.MTS
{
    public class MtsBetProvider : IBetProvider
    {
        public ChannelType ChannelType => ChannelType.SR;

        private readonly IMTSAuthRepository _auth;
        private readonly IMTSMaxStakeRepository _maxStake;
        private readonly IParlayBonusService _parlayBonusService;

        private readonly IConfiguration _configuration;
        private readonly ILogger<MtsBetProvider> _logger;

        private IMtsSdk? _mts;
        private IBuilderFactory _factory;

        public MtsBetProvider(
            IConfiguration configuration,
            ILogger<MtsBetProvider> logger,
            IMTSAuthRepository auth,
            IMTSMaxStakeRepository maxStake,
            IParlayBonusService parlayBonusService)
        {
            _configuration = configuration;
            _logger = logger;
            _auth = auth;
            _maxStake = maxStake;
            _parlayBonusService = parlayBonusService;
        }

        private void EnsureInitialized()
        {
            if (_mts != null) return;

            _logger.LogInformation("Starting MTS SDK");

            var config = MtsSdk.GetConfiguration();
            _mts = new MtsSdk(config);
            _mts.Open();
        }

        public async Task<BetProviderResult> ValidateAsync(OnBetPlacement bet, CancellationToken ct)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Accepted (MTS disabled)"
                };

            if (bet.BetSelections.Any(c => c.BetSelection.SportId.IsVirtualSportId()))
                return new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Accepted (MTS disabled for VS)"
                };

            EnsureInitialized();

            var result = ValidateToMTS(bet); // existing logic moved here

            return new BetProviderResult
            {
                Acceptance = result.Item1.ToBetAcceptance(),
                Reason = result.Item2
            };
        }

        public async Task<bool> IsAvailableAsync(BetRequestDTO request)
        {
            var token = await _auth.GetMTSAuthorizationToken();
            return token.StatusCode == StatusCode.Success;
        }

        public async Task<ProviderStakeResult> GetMaxStakeAsync(BetRequestDTO request)
        {
            var token = await _auth.GetMTSAuthorizationToken();
            if (token.StatusCode != StatusCode.Success)
                return new ProviderStakeResult
                {
                    IsSuccess = false,
                    MaxStake = 0,
                    StatusCode = (int)token.StatusCode,
                    Message = "MTS_AUTH_FAILED"
                };


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

            var result = await _maxStake.GetMTSMaxStake(
                token.Result.Token,
                mtsConfigRequest,
                request.CompanyShare,
                request.IsBetBuilder,
                request.Odds);

            return result.StatusCode == StatusCode.Success
                ? new ProviderStakeResult
                {
                    IsSuccess = true,
                    MaxStake = result.Result.Stake,
                    StatusCode = (int)result.StatusCode,
                    Message = result.StatusCode.ToString()
                }
                : new ProviderStakeResult
                {
                    IsSuccess = false,
                    MaxStake = 0,
                    StatusCode = (int)result.StatusCode,
                    Message = !string.IsNullOrEmpty(result.Message) ? result.Message : "MTS_MAXSTAKE_FAILED"
                };
        }

        public void ProcessCancellation(Guid betId, string limitId, string clientCode, string correlationId, bool isBackOfficeTrigger = false)
        {
            if (Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
            {
                try
                {
                    _factory = _mts.BuilderFactory;

                    var betCancel = isBackOfficeTrigger
                        ? _factory.CreateTicketCancelBuilder().BuildTicket(betId.ToString().ToUpper(), Convert.ToInt32(_configuration["Custom:BookmakerId"]), TicketCancellationReason.BookmakerBackofficeTriggered)
                        : _factory.CreateTicketCancelBuilder().BuildTicket(betId.ToString().ToUpper(), Convert.ToInt32(_configuration["Custom:BookmakerId"]), TicketCancellationReason.TimeoutTriggered);

                    var payload = new
                    {
                        betId = betId,
                        payload = JsonSerializer.Serialize(betCancel)
                    };

                    _logger.LogInformation("ProcessCancellation (Request): {@payload}", payload);
                    var response = _mts.SendTicketCancelBlocking(betCancel);
                    _logger.LogInformation("ProcessCancellation (Response): {@response}", response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("ProcessCancellation Error: {@exception}", ex);
                }
            }
        }
        public Task ProcessSettlement(OnBetSettlement bet, string correlationId)
        {
            // No-op: MTS settlements are not forwarded.
            return Task.CompletedTask;
        }

        public Task ProcessCashout(OnBetSettlement bet, CashoutType cashoutType, string correlationId)
        {
            // No-op: MTS is notified pre-acceptance via ValidateCashoutAsync
            // (SendTicketCashoutBlocking executes the cashout at MTS).
            return Task.CompletedTask;
        }

        // Pre-acceptance cash-out gate. Unlike the GTS assessment (read-only), the MTS
        // ticket cashout EXECUTES the cashout at MTS, which is why this must run before
        // the platform accepts and pays out (same behavior the CashOut.Service had inline).
        public Task<BetProviderResult> ValidateCashoutAsync(OnCashoutBetSettlement message, Bet bet, List<BetSelection> selections, bool isFullCashout, decimal cashoutPercent, decimal prevCashoutAmount)
        {
            if (!Convert.ToBoolean(_configuration["Custom:IsProviderEnabled"]))
                return Task.FromResult(new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Cashout Accepted (MTS disabled)"
                });

            if (selections.Any(s => s.SportId.IsVirtualSportId()))
                return Task.FromResult(new BetProviderResult
                {
                    Acceptance = BetAcceptance.Accepted,
                    Reason = "1024 - Cashout Accepted (MTS disabled for VS)"
                });

            EnsureInitialized();

            var result = isFullCashout
                ? SendCashoutToMTS(message)
                : SendPartialCashoutToMTS(message, cashoutPercent, prevCashoutAmount);

            return Task.FromResult(new BetProviderResult
            {
                Acceptance = result.Item1 == CashoutAcceptance.Accepted ? BetAcceptance.Accepted : BetAcceptance.Rejected,
                Reason = result.Item2
            });
        }

        private Tuple<CashoutAcceptance, string> SendCashoutToMTS(OnCashoutBetSettlement message)
        {
            try
            {
                _factory = _mts.BuilderFactory;

                var cashoutStake = (long)0;

                if (message.ClientCode.ToUpper() == "RKP")
                    cashoutStake = (long)(message.CashOutStake * .10m) * 10000;
                else
                    cashoutStake = (long)(message.CashOutStake * 10000);

                var betCashout = _factory.CreateTicketCashoutBuilder().SetTicketId(message.BetId.ToString().ToUpper()).SetCashoutStake(cashoutStake).BuildTicket();

                var response = _mts.SendTicketCashoutBlocking(betCashout);

                return new Tuple<CashoutAcceptance, string>(response.Status, $"{response.Reason.Code} - {response.Reason.Message}");
            }
            catch (Exception ex)
            {
                return new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        private Tuple<CashoutAcceptance, string> SendPartialCashoutToMTS(OnCashoutBetSettlement message, decimal cashoutPercent, decimal prevCashoutAmount)
        {
            try
            {
                _factory = _mts.BuilderFactory;

                var cashoutStake = (long)0;
                var cashoutStakePercent = (int)0;

                if (message.ClientCode.ToUpper() == "RKP")
                {
                    cashoutStake = (long)((prevCashoutAmount + message.CashOutStake) * .10m) * 10000;
                    cashoutStakePercent = (int)((cashoutPercent * 100) * .10m) * 10000;
                }
                else
                {
                    cashoutStake = (long)((prevCashoutAmount + message.CashOutStake) * 10000);
                    cashoutStakePercent = (int)((cashoutPercent * 100) * 10000);
                }

                var betCashout = _factory.CreateTicketCashoutBuilder().SetTicketId(message.BetId.ToString().ToUpper()).SetCashoutStake(cashoutStake).SetCashoutPercent(cashoutStakePercent).BuildTicket();

                var response = _mts.SendTicketCashoutBlocking(betCashout);

                return new Tuple<CashoutAcceptance, string>(response.Status, $"{response.Reason.Code} - {response.Reason.Message}");
            }
            catch (Exception ex)
            {
                return new Tuple<CashoutAcceptance, string>(CashoutAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        private Tuple<TicketAcceptance, string> ValidateToMTS(OnBetPlacement message)
        {
            try
            {
                _factory = _mts.BuilderFactory;

                TicketAcceptance betStatus;
                string reason;

                var isValidIP = IPAddress.TryParse(message.Bet.IPAddress, out IPAddress ipAddress);

                if (!isValidIP)
                    ipAddress = IPAddress.Loopback;

                var bet = _factory.CreateTicketBuilder()
                    .SetTicketId(message.Bet.BetId.ToString().ToUpper())
                    .SetOddsChange(Sportradar.MTS.SDK.Entities.Enums.OddsChangeType.None)
                    .SetSender(_factory.CreateSenderBuilder()
                    .SetSenderChannel((SenderChannel)Enum.Parse(typeof(SenderChannel), message.Bet.Channel))
                    .SetLimitId(Convert.ToInt32(message.Bet.LimitId == "0" ? "2183" : message.Bet.LimitId))
                    .SetCurrency(message.Bet.CurrencyCode.ToString())
                    .SetEndCustomer(_factory.CreateEndCustomerBuilder()
                    .SetId($"{message.Bet.ClientCode}{message.Bet.UserId.Replace("-", "")}")
                    .SetConfidence(10000)
                    .SetIp(ipAddress)
                    .SetLanguageId(message.Bet.LanguageCode.ToString())
                    .SetDeviceId($"{message.Bet.ClientCode}{message.Bet.UserId.Replace("-", "")}")
                    .Build())
                    .Build())
                    .AddBet(BuildBet(message.Bet, message.BetSelections.Select(s => s.BetSelection).ToList(), message.IsBetBuilder, message.Odds))
                    .BuildTicket();

                var response = _mts.SendTicketBlocking(bet);

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<TicketAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateToMTS Error : {@exception}", ex);
                return new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"10000 - {ex.Message}");
            }
        }

        private IBet BuildBet(Bet betData, List<BetSelection> betSelectionData, bool isBetBuilder, decimal calculatedOdds)
        {
            //var companyStake = (0.0001m * betData.ExchangeRate) * 10000;

            //if (betData.CompanyShare > 0)
            //    companyStake = (betData.TrueStake * betData.CompanyShare) * 10000;

            //if (betData.CompanyShare == 0)
            //    companyStake = betData.TrueStake * 10000;

            //var companyStake = betData.TrueStake * 10000;
            //var stake = (betData.TrueStake - (betData.TrueStake * betData.WinningsTax)) * 10000;

            var stake = 0m;
            var entireStake = betData.TrueStake * 10000;

            if (betData.ClientCode.ToUpper() == "RKP")
                stake = (betData.TrueStake * .10m) * 10000;
            else
                stake = betData.TrueStake * 10000;

            #region Get Parlay Bonus
            var bonus = 0m;
            var parlayBonus = 0m;
            var hasBonus = false;
            BetBonusDescription bonusDescription = BetBonusDescription.Other;
            // parlay bonus: bet should be Won by default

            var hasVirtualSport = betSelectionData.Any(s => s.SportId.IsVirtualSportId()) ? true : false;

            if (isBetBuilder == false && hasVirtualSport == false && betSelectionData.Count >= 3 && betData.IsFreeBet == false)
            {
                var (hasParlayBonus, bonusConfig) =
                    _parlayBonusService.GetParlayBonus(
                        Guid.Parse(betData.ClientId),
                        betData.CurrencyCode,
                        betData.BetDate,
                        betSelectionData,
                        betData.Payout);

                if (hasParlayBonus == true)
                {
                    parlayBonus = bonusConfig.ParlayBonus;
                    bonus = betData.Payout * bonusConfig.ParlayBonus;
                    bonus = bonus * 10000;

                    hasBonus = bonus == 0 ? false : true;
                    bonusDescription = BetBonusDescription.AccaBonus;
                }
            }

            if (betData.BetTypeId == (int)BetType.PriceBoost) //boosted bonus
            {
                var totalBoostedPayout = betData.Odds * stake;
                var totalOriginalPayout = betData.OddsRef * stake;

                bonus = bonus + (totalBoostedPayout - totalOriginalPayout);

                hasBonus = bonus == 0 ? false : true;
                bonusDescription = BetBonusDescription.OddsBooster;
            }

            #endregion

            var selection = hasBonus ? _factory.CreateBetBuilder()
                                    .SetBetId(betData.BetId.ToString().ToUpper())
                                    .SetBetBonus(Convert.ToInt64(bonus), BetBonusMode.All, BetBonusType.Total, bonusDescription, BetBonusPaidAs.Cash)
                                    .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                    .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                    .AddSelectedSystem(betSelectionData.Count)
                                    :
                                        isBetBuilder ? _factory.CreateBetBuilder()
                                            .SetCustomBet(true)
                                            .SetCalculationOdds(Convert.ToInt32(calculatedOdds * 10000))
                                            .SetBetId(betData.BetId.ToString().ToUpper())
                                            .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                            .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                            .AddSelectedSystem(betSelectionData.Count)
                                            :
                                                _factory.CreateBetBuilder()
                                                .SetBetId(betData.BetId.ToString().ToUpper())
                                                .SetStake(Convert.ToInt64(stake), StakeType.Total)
                                                .SetEntireStake(Convert.ToInt64(entireStake), StakeType.Total)
                                                .AddSelectedSystem(betSelectionData.Count);

            foreach (var betSelection in betSelectionData)
            {
                if (betData.BetTypeId == (int)BetType.PriceBoost)
                {
                    selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betSelection.OddsRef * 10000))
                                               .SetBoostedOdds(Convert.ToInt32(betSelection.Odds * 10000))
                                               .Build());
                }
                else
                {
                    selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betSelection.Odds * 10000))
                                               .Build());
                }

            }

            return selection.Build();
        }


        public async ValueTask DisposeAsync()
        {
            _mts?.Dispose();
        }
    }

}

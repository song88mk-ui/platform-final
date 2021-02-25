using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Betting;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Transactions;

namespace Platform.Placement.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private IBuilderFactory _factory;
        private readonly IConfiguration _configuration;
        public BettingRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ProcesssBetPlacement(OnBetPlacement message, IMtsSdk mts, IMessageBroker messageBroker, string correlationId)
        {
            var payload = new OnBetPlacement();

            var result = Convert.ToBoolean(_configuration["Custom:IsMTSEnabled"]) ? 
                message.BetSelections.Any(c => c.SportId > 1000) ? new Tuple<TicketAcceptance, string>(TicketAcceptance.Accepted, "Accepted (MTS disabled for VS)") :
                ValidateToMTS(message, mts) : new Tuple<TicketAcceptance, string>(TicketAcceptance.Accepted, "Accepted (MTS disabled)");

            if (result.Item1 == TicketAcceptance.Accepted)
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        try
                        {
                            var bet = context.Bet.Find(message.BetId);
                            if (bet != null)
                            {
                                var betStatus = BetStatus.Running;

                                bet.BetStatusId = (int)betStatus;
                                bet.BetStatusName = betStatus.ToString();
                                bet.Reason = result.Item2;

                                var betEntity = context.Bet.Update(bet).Entity;
                                payload.Bet = betEntity;

                                context.BetAudit.Add(new BetAudit
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
                                    SettlementDateOffset = betEntity.SettlementDateOffset
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == betEntity.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = betEntity.BetStatusId;
                                    betSelection.BetSelectionStatusName = betEntity.BetStatusName;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                    payload.BetSelections.Add(betSelectionEntity);

                                    context.BetSelectionAudit.Add(new BetSelectionAudit
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
                                        CorrelationId = betSelectionEntity.CorrelationId
                                    });
                                }

                                context.SaveChanges();
                            }
                            scope.Complete();

                            messageBroker.Publish(payload, "OnBetPlacement", "r.bet.operator", correlationId);
                            messageBroker.Publish(payload, "OnBetPlacement", "r.bet.reports", correlationId);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
            else
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<BettingContext>().UseSqlServer(_configuration["ConnectionStrings:Betting"]);

                    using (var context = new BettingContext(optionsBuilder.Options))
                    {
                        try
                        {
                            var bet = context.Bet.Find(message.BetId);
                            if (bet != null)
                            {
                                var betStatus = BetStatus.Rejected;

                                bet.BetStatusId = (int)betStatus;
                                bet.BetStatusName = betStatus.ToString();
                                bet.Reason = result.Item2;

                                var betEntity = context.Bet.Update(bet).Entity;
                                payload.Bet = betEntity;

                                context.BetAudit.Add(new BetAudit
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
                                    SettlementDateOffset = betEntity.SettlementDateOffset
                                });

                                var betSelections = context.BetSelection.Where(c => c.BetId == bet.BetId).ToList();

                                foreach (var betSelection in betSelections)
                                {
                                    betSelection.BetSelectionStatusId = bet.BetStatusId;
                                    betSelection.BetSelectionStatusName = bet.BetStatusName;
                                    betSelection.IsSettled = true;

                                    var betSelectionEntity = context.BetSelection.Update(betSelection).Entity;
                                    payload.BetSelections.Add(betSelectionEntity);

                                    context.BetSelectionAudit.Add(new BetSelectionAudit
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
                                        CorrelationId = betSelectionEntity.CorrelationId
                                    });
                                }

                                context.SaveChanges();
                            }
                            scope.Complete();

                            messageBroker.Publish(payload, "OnBetTimeout", "r.bet.cancellation", correlationId);
                            messageBroker.Publish(payload, "OnBetRejection", "r.bet.reports", correlationId);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
        }
        private Tuple<TicketAcceptance, string> ValidateToMTS(OnBetPlacement message, IMtsSdk mts)
        {
            try
            {
                _factory = mts.BuilderFactory;

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
                    .SetDeviceId($"{message.Bet.ClientCode}-{message.Bet.UserId}")
                    .Build())
                    .Build())
                    .AddBet(BuildBet(message.Bet, message.BetSelections))
                    //.SetTestSource(message.IsTestAccount)
                    .BuildTicket();

                var response = mts.SendTicketBlocking(bet);

                betStatus = response.Status;
                reason = $"{response.Reason.Code} - {response.Reason.Message}";

                return new Tuple<TicketAcceptance, string>(betStatus, reason);
            }
            catch (Exception ex)
            {
                return new Tuple<TicketAcceptance, string>(TicketAcceptance.Rejected, $"{ex.Message}");
            }
        }
        private IBet BuildBet(Bet betData, List<BetSelection> betSelectionData)
        {
            var companyStake = (0.001m * betData.ExchangeRate) * 10000;

            if (betData.CompanyShare > 0)
                companyStake = (betData.TrueStake * betData.CompanyShare) * 10000;

            if (betData.CompanyShare == 0)
                companyStake = betData.TrueStake * 10000;

            var selection = _factory.CreateBetBuilder()
                                    .SetBetId(betData.BetId.ToString().ToUpper())
                                    .SetStake(Convert.ToInt64(companyStake), StakeType.Total)
                                    .SetEntireStake(Convert.ToInt64((betData.TrueStake * 10000)), StakeType.Total)
                                    .AddSelectedSystem(betSelectionData.Count);

            foreach (var betSelection in betSelectionData)
            {
                selection.AddSelection(_factory.CreateSelectionBuilder()
                                               .SetEventId(betSelection.FixtureSpecifier)
                                               .SetId(betSelection.SelectionSpecifier)
                                               .SetOdds(Convert.ToInt32(betData.Odds * 10000))
                                               .Build());
            }

            return selection.Build();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Platform.Shared.Data.Reports.Entities;
using Platform.Shared.Data.Reports;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Transactions;
using System;
using Platform.AgentReports.Service.Models;
using Platform.Shared.Data.Betting.Entities;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Platform.AgentReports.Service.Enums;
using Platform.Shared.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Platform.AgentReports.Service.Repository
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly IConfiguration _configuration;
        private static string _agentUrl = string.Empty;
        public ReportsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _agentUrl = _configuration["API:AgentAPI"];
        }
        public void ProcessBetPlacement(OnBetPlacement message)
        {
            var agents = GetAgentSettingsFinal(message).OrderBy(o => o.Hierarchy).ToList();

            if (agents.Count == 0)
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<AgentReportsContext>().UseSqlServer(_configuration["ConnectionStrings:Reports"]);

                using (var context = new AgentReportsContext(optionsBuilder.Options))
                {
                    foreach (var agent in agents)
                    {
                        var bet = context.Bet.Find(agent.AgentId, message.Bet.BetId);

                        if (bet == null)
                            bet = context.Bet.Add(ConvertToBetEntityInsertFinal(message, agent, agents)).Entity;
                        else
                        {
                            bet.Payout = message.Bet.Payout;
                            bet.WinLoss = message.Bet.WinLoss;
                            bet.StatusId = message.Bet.BetStatusId;
                            bet.StatusName = message.Bet.BetStatusName;
                            bet.Reason = message.Bet.Reason;
                            bet.ReceivePositionTaking = GetReceivePTFinal(message.Bet.Stake, message.Bet.Payout, message.Bet.BetStatusId, agent);
                            context.Bet.Update(bet);
                        }

                        context.BetTransaction.Add(ConvertToBetTransactionEntityInsertFinal(message, agent));

                        foreach (var selection in message.BetSelections)
                        {
                            var betSelection = context.BetSelection.Find(agent.AgentId, selection.BetId, selection.BetSelectionId);

                            if (betSelection == null)
                                context.BetSelection.Add(ConvertToBetSelectionEntityInsertFinal(selection, agent));
                            else
                            {
                                betSelection.StatusId = selection.BetSelectionStatusId;
                                betSelection.StatusName = selection.BetSelectionStatusName;
                                betSelection.IsSettled = selection.IsSettled;
                                betSelection.IsResettled = selection.IsResettled;

                                context.BetSelection.Update(betSelection);
                            }

                            context.BetSelectionTransaction.Add(ConvertToBetSelectionTransactionEntityInsertFinal(selection, agent));
                        }

                        AddOrUpdateRunningBalanceFinal(context, message.Bet.TrueStake, message.Bet.CurrencyCode, message.Bet.BetId, message.Bet.BetStatusId, message.Bet.BetStatusName, message.Bet.BetDate, agent, 0, agents);
                    }
                    context.SaveChanges();
                }
                scope.Complete();
            }
        }

        public void ProcessBetFullCashOut(OnBetSettlement message)
        {
            var agents = GetAgentSettingsFinal(message).OrderBy(o => o.Hierarchy).ToList();

            if (agents.Count == 0)
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<AgentReportsContext>().UseSqlServer(_configuration["ConnectionStrings:Reports"]);

                using (var context = new AgentReportsContext(optionsBuilder.Options))
                {
                    foreach (var agent in agents)
                    {
                        var bet = context.Bet.Find(agent.AgentId, message.Bet.BetId);

                        if (bet != null)
                        {
                            bet.Payout = message.Bet.Payout;
                            bet.WinLoss = message.Bet.WinLoss;
                            bet.StatusId = message.Bet.BetStatusId;
                            bet.StatusName = message.Bet.BetStatusName;
                            bet.Reason = message.Bet.Reason;
                            bet.ReceivePositionTaking = GetReceivePTFinal(message, agent);
                            context.Bet.Update(bet);

                            context.BetTransaction.Add(ConvertToBetTransactionEntityInsertFinal(message, agent));

                            foreach (var selection in message.BetSelections)
                            {
                                var betSelection = context.BetSelection.Find(agent.AgentId, selection.BetId, selection.BetSelectionId);

                                if (betSelection == null)
                                    context.BetSelection.Add(ConvertToBetSelectionEntityInsertFinal(selection, agent));
                                else
                                {
                                    betSelection.StatusId = selection.BetSelectionStatusId;
                                    betSelection.StatusName = selection.BetSelectionStatusName;
                                    betSelection.IsSettled = selection.IsSettled;
                                    betSelection.IsResettled = selection.IsResettled;

                                    context.BetSelection.Update(betSelection);
                                }

                                context.BetSelectionTransaction.Add(ConvertToBetSelectionTransactionEntityInsertFinal(selection, agent));
                            }

                            AddOrUpdateWinLossFinal(context, message, agent, agents, bet);
                        }
                    }
                    context.SaveChanges();
                }
                scope.Complete();
            }
        }

        public void ProcessBetSettlement(OnBetSettlement message)
        {
            var agents = GetAgentSettingsFinal(message).OrderBy(o => o.Hierarchy).ToList();

            if (agents.Count == 0)
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<AgentReportsContext>().UseSqlServer(_configuration["ConnectionStrings:Reports"]);

                using (var context = new AgentReportsContext(optionsBuilder.Options))
                {
                    foreach (var agent in agents)
                    {
                        var bet = context.Bet.Find(agent.AgentId, message.Bet.BetId);

                        if (bet != null)
                        {
                            var currentStatusId = bet.StatusId;

                            bet.Payout = message.Bet.Payout;
                            bet.WinLoss = message.Bet.WinLoss;
                            bet.StatusId = message.Bet.BetStatusId;
                            bet.StatusName = message.Bet.BetStatusName;
                            bet.Reason = message.Bet.Reason;
                            bet.ReceivePositionTaking = GetReceivePTFinal(message, agent);
                            context.Bet.Update(bet);

                            context.BetTransaction.Add(ConvertToBetTransactionEntityInsertFinal(message, agent));

                            foreach (var selection in message.BetSelections)
                            {
                                var betSelection = context.BetSelection.Find(agent.AgentId, selection.BetId, selection.BetSelectionId);

                                if (betSelection == null)
                                    context.BetSelection.Add(ConvertToBetSelectionEntityInsertFinal(selection, agent));
                                else
                                {
                                    betSelection.StatusId = selection.BetSelectionStatusId;
                                    betSelection.StatusName = selection.BetSelectionStatusName;
                                    betSelection.IsSettled = selection.IsSettled;
                                    betSelection.IsResettled = selection.IsResettled;

                                    context.BetSelection.Update(betSelection);
                                }

                                context.BetSelectionTransaction.Add(ConvertToBetSelectionTransactionEntityInsertFinal(selection, agent));
                            }

                            var validBetStatusId = new int[] { (int)BetStatus.Running };

                            if (validBetStatusId.Contains(currentStatusId))
                                AddOrUpdateWinLossFinal(context, message, agent, agents, bet);
                        }
                    }

                    context.SaveChanges();
                }
                scope.Complete();
            }
        }

        public void ProcessBetCancellation(OnBetSettlement message)
        {
            var agents = GetAgentSettingsFinal(message).OrderBy(o => o.Hierarchy).ToList();

            if (agents.Count == 0)
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<AgentReportsContext>().UseSqlServer(_configuration["ConnectionStrings:Reports"]);

                using (var context = new AgentReportsContext(optionsBuilder.Options))
                {
                    foreach (var agent in agents)
                    {
                        var bet = context.Bet.Find(agent.AgentId, message.Bet.BetId);

                        if (bet != null)
                        {
                            bet.Payout = message.Bet.Payout;
                            bet.WinLoss = message.Bet.WinLoss;
                            bet.StatusId = message.Bet.BetStatusId;
                            bet.StatusName = message.Bet.BetStatusName;
                            bet.Reason = message.Bet.Reason;
                            bet.ReceivePositionTaking = GetReceivePTFinal(message, agent);
                            context.Bet.Update(bet);

                            context.BetTransaction.Add(ConvertToBetTransactionEntityInsertFinal(message, agent));

                            foreach (var selection in message.BetSelections)
                            {
                                var betSelection = context.BetSelection.Find(agent.AgentId, selection.BetId, selection.BetSelectionId);

                                if (betSelection == null)
                                    context.BetSelection.Add(ConvertToBetSelectionEntityInsertFinal(selection, agent));
                                else
                                {
                                    betSelection.StatusId = selection.BetSelectionStatusId;
                                    betSelection.StatusName = selection.BetSelectionStatusName;
                                    betSelection.IsSettled = selection.IsSettled;
                                    betSelection.IsResettled = selection.IsResettled;

                                    context.BetSelection.Update(betSelection);
                                }

                                context.BetSelectionTransaction.Add(ConvertToBetSelectionTransactionEntityInsertFinal(selection, agent));
                            }

                            AddOrUpdateWinLossFinal(context, message, agent, agents, bet);
                        }
                    }
                    context.SaveChanges();
                }
                scope.Complete();
            }
        }

        public void ProcessCasinoBetSettlement(OnCasinoBetSettlement message)
        {
            var agents = GetCasinoAgentSettingsFinal(message.Category, message.UserId, message.BetDate).OrderBy(o => o.Hierarchy).ToList();

            if (agents.Count == 0)
                return;

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<AgentReportsContext>().UseSqlServer(_configuration["ConnectionStrings:Reports"]);

                using (var context = new AgentReportsContext(optionsBuilder.Options))
                {
                    var product = UpdateProductLookup(context, message.Category, message.GameId);

                    foreach (var agent in agents)
                    {
                        AddOrUpdateCasinoWinLossFinal(context, message, agent, agents, product);
                    }


                    context.SaveChanges();
                }
                scope.Complete();
            }
        }
        private List<AgentSettings> GetAgentSettingsFinal(OnBetPlacement message)
        {
            var agents = new List<AgentSettings>();
            var categoryId = 0;
            var subCategoryId = 0;
            var betSelectionCount = 0;

            if (message.BetSelections.Count == 1)
            {
                if (message.BetSelections[0].SportId == 1)
                {
                    categoryId = (int)Category.Soccer;
                    subCategoryId = message.BetSelections[0].FixtureStatusId == (int)FixtureStatus.Live ? 2 : 1;
                    betSelectionCount = message.BetSelections.Count;
                }
                else if (message.BetSelections[0].SportId == 21)
                {
                    categoryId = (int)Category.Cricket;
                    subCategoryId = message.BetSelections[0].FixtureStatusId == (int)FixtureStatus.Live ? 2 : 1;
                    betSelectionCount = message.BetSelections.Count;
                }
                else
                {
                    categoryId = (int)Category.Sports;
                    subCategoryId = 0;
                    betSelectionCount = message.BetSelections.Count;
                }
            }

            if (message.BetSelections.Count > 1)
            {
                categoryId = (int)Category.Parlay;
                subCategoryId = 0;
                betSelectionCount = message.BetSelections.Count;
            }

            var url = $"{_agentUrl}/api/v3/ptc2/{message.Bet.UserId}/{categoryId}/{subCategoryId}/{betSelectionCount}?transactionDate={message.Bet.BetDate.ToString("s")}";
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = httpClient.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    var resultList = System.Text.Json.JsonSerializer.Deserialize<BaseResponse<List<AgentSettingsResponse>>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    foreach (var result in resultList.Result)
                    {
                        agents.Add(new AgentSettings
                        {
                            AgentId = result.AgentId,
                            AgentUsername = result.AgentUsername,
                            Username = result.Username,
                            PositionTaking = result.PositionTaking,
                            Commission = result.Commission,
                            UserType = GetUserType(result.UserType),
                            UplineUserId = result.UplineUserId,
                            IsCashMarket = result.IsCashMarket,
                            CurrencyCode = result.CurrencyCode,
                            Hierarchy = result.Hierarchy
                        });
                    }
                }
            }

            return agents;
        }

        private List<AgentSettings> GetCasinoAgentSettingsFinal(string category, string userId, DateTime transactionDate)
        {
            var agents = new List<AgentSettings>();
            var categoryType = GetCategory(category);
            var categoryId = (int)categoryType;
            var subCategoryId = 0;
            var betSelectionCount = 1;


            var url = $"{_agentUrl}/api/v3/ptc2/{userId}/{categoryId}/{subCategoryId}/{betSelectionCount}?transactionDate={transactionDate.ToString("s")}";
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = httpClient.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    var resultList = System.Text.Json.JsonSerializer.Deserialize<BaseResponse<List<AgentSettingsResponse>>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    foreach (var result in resultList.Result)
                    {
                        agents.Add(new AgentSettings
                        {
                            AgentId = result.AgentId,
                            AgentUsername = result.AgentUsername,
                            Username = result.Username,
                            PositionTaking = result.PositionTaking,
                            Commission = result.Commission,
                            UserType = GetUserType(result.UserType),
                            UplineUserId = result.UplineUserId,
                            IsCashMarket = result.IsCashMarket,
                            CurrencyCode = result.CurrencyCode,
                            Hierarchy = result.Hierarchy
                        });
                    }
                }
            }

            return agents;
        }

        private Shared.Data.Reports.Entities.Bet ConvertToBetEntityInsertFinal(OnBetPlacement message, AgentSettings agent, List<AgentSettings> agents)
        {
            var agentId = agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.UplineUserId).FirstOrDefault();

            return new Shared.Data.Reports.Entities.Bet
            {
                ParentUserId = agent.AgentId,
                BetId = message.Bet.BetId,
                UserId = Convert.ToInt32(message.Bet.UserId),
                Username = agent.Username,
                AgentId = agentId,
                AgentUsername = agents.Where(c => c.AgentId == agentId).Select(s => s.AgentUsername).FirstOrDefault(),
                UserTypeId = (int)Shared.Enums.UserType.Player,
                PositionTaking = agent.PositionTaking,
                Commission = agent.Commission,
                Stake = message.Bet.Stake,
                TrueStake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                OddsTypeId = message.Bet.OddsTypeId,
                OddsTypeName = message.Bet.OddsTypeName,
                Payout = message.Bet.Payout,
                WinLoss = message.Bet.WinLoss,
                StatusId = message.Bet.BetStatusId,
                StatusName = message.Bet.BetStatusName,
                Reason = message.Bet.Reason,
                CurrencyCode = message.Bet.CurrencyCode,
                ExchangeRate = message.Bet.ExchangeRate,
                IPAddress = message.Bet.IPAddress,
                Count = message.BetSelections.Count,
                SportId = message.BetSelections.First().SportId,
                SportName = message.BetSelections.First().SportName,
                SegmentId = message.BetSelections.First().SegmentId,
                SegmentName = message.BetSelections.First().SegmentName,
                LeagueId = message.BetSelections.First().LeagueId,
                LeagueName = message.BetSelections.First().LeagueName,
                FixtureId = message.BetSelections.First().FixtureId,
                FixtureName = message.BetSelections.First().FixtureName,
                FixtureStatusId = message.BetSelections.First().FixtureStatusId,
                FixtureStatusName = message.BetSelections.First().FixtureStatusName,
                FixtureStartDate = message.BetSelections.First().FixtureStartDate,
                Score = message.BetSelections.First().BetScore,
                MarketTemplateGroupId = string.Empty,
                MarketTemplateGroupName = string.Empty,
                MarketTemplateId = message.BetSelections.First().MarketTemplateId,
                MarketId = message.BetSelections.First().MarketId,
                MarketName = message.BetSelections.First().MarketName,
                Line = message.BetSelections.First().Line,
                SelectionTemplateId = message.BetSelections.First().SelectionTemplateId,
                SelectionId = message.BetSelections.First().SelectionId,
                SelectionName = message.BetSelections.First().SelectionName,
                ProductId = (int)Shared.Enums.Product.Sportsbook,
                ProductName = Shared.Enums.Product.Sportsbook.ToString(),
                ReceivePositionTaking = GetReceivePTFinal(message.Bet.Stake, message.Bet.Payout, message.Bet.BetStatusId, agent),
                CasinoDetails = string.Empty,
                GameProvider = 0,
                TransactionDate = message.Bet.BetDate,
                TransactionDateOffset = message.Bet.BetDate.AddHours(-4),
                ProviderBrandId = 0,
                ProviderBrandName = string.Empty,
                GameId = 0,
                GameName = string.Empty,
                MarketTypeId = string.Empty,
                MarketGroupId = string.Empty
            };
        }


        private decimal GetReceivePTFinal(decimal stake, decimal payout, int betStatusId, AgentSettings agent)
        {
            var result = 0m;

            var ptStake = agent.PositionTaking * stake;
            var ptPayout = agent.PositionTaking * payout;

            switch ((BetStatus)betStatusId)
            {
                case BetStatus.Won:
                case BetStatus.HalfWon:
                case BetStatus.HalfLost:
                case BetStatus.CashedOut:
                    if (agent.UserType == Shared.Enums.UserType.Player)
                        result = payout > stake ? payout : payout * -1;
                    else
                        result = ptPayout > ptStake ? ptPayout * -1 : ptPayout;
                    break;
                case BetStatus.Lost:
                    if (agent.UserType == Shared.Enums.UserType.Player)
                        result = stake * -1;
                    else
                        result = ptStake;
                    break;
                case BetStatus.Void:
                case BetStatus.Rejected:
                case BetStatus.Cancelled:
                    result = 0m;
                    break;
                default:
                    result = ptStake;
                    break;
            }

            return result;
        }
        private BetTransaction ConvertToBetTransactionEntityInsertFinal(OnBetPlacement message, AgentSettings agent)
        {
            return new BetTransaction
            {
                ParentUserId = agent.AgentId,
                BetId = message.Bet.BetId,
                UserId = Convert.ToInt32(message.Bet.UserId),
                Username = agent.Username,
                UserTypeId = (int)Shared.Enums.UserType.Player,
                PositionTaking = agent.PositionTaking,
                Commission = agent.Commission,
                Stake = message.Bet.Stake,
                TrueStake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                OddsTypeId = message.Bet.OddsTypeId,
                OddsTypeName = message.Bet.OddsTypeName,
                Payout = message.Bet.Payout,
                WinLoss = message.Bet.WinLoss,
                StatusId = message.Bet.BetStatusId,
                StatusName = message.Bet.BetStatusName,
                Reason = message.Bet.Reason,
                CurrencyCode = message.Bet.CurrencyCode,
                IPAddress = message.Bet.IPAddress,
                Count = message.BetSelections.Count,
                ExchangeRate = message.Bet.ExchangeRate,
                TransactionDate = message.Bet.BetDate,
                TransactionDateOffset = message.Bet.BetDate.AddHours(-4)
            };
        }

        private Shared.Data.Reports.Entities.BetSelection ConvertToBetSelectionEntityInsertFinal(Shared.Data.Betting.Entities.BetSelection betSelection, AgentSettings agent)
        {
            return new Shared.Data.Reports.Entities.BetSelection
            {
                ParentUserId = agent.AgentId,
                BetId = betSelection.BetId,
                BetSelectionId = betSelection.BetSelectionId,
                UserId = Convert.ToInt32(betSelection.UserId),
                Username = agent.Username,
                UserTypeId = (int)Shared.Enums.UserType.Player,
                SportId = betSelection.SportId,
                SportName = betSelection.SportName,
                SegmentId = betSelection.SegmentId,
                SegmentName = betSelection.SegmentName,
                LeagueId = betSelection.LeagueId,
                LeagueName = betSelection.LeagueName,
                FixtureId = betSelection.FixtureId,
                FixtureName = betSelection.FixtureName,
                MarketId = betSelection.MarketId,
                MarketName = betSelection.MarketName,
                SelectionId = betSelection.SelectionId,
                SelectionName = betSelection.SelectionName,
                FixtureStatusId = betSelection.FixtureStatusId,
                FixtureStatusName = betSelection.FixtureStatusName,
                Line = betSelection.Line,
                Odds = betSelection.Odds,
                StatusId = betSelection.BetSelectionStatusId,
                StatusName = betSelection.BetSelectionStatusName,
                IsSettled = betSelection.IsSettled,
                IsResettled = betSelection.IsResettled,
                FixtureStartDate = betSelection.FixtureStartDate,
                MarketTemplateGroupId = string.Empty,
                MarketTemplateId = betSelection.MarketTemplateId,
                SelectionTemplateId = betSelection.SelectionTemplateId,
                Score = betSelection.BetScore,
                IsBinary = betSelection.IsBinary
            };
        }
        private BetSelectionTransaction ConvertToBetSelectionTransactionEntityInsertFinal(Shared.Data.Betting.Entities.BetSelection betSelection, AgentSettings agent)
        {
            return new BetSelectionTransaction
            {
                ParentUserId = agent.AgentId,
                BetId = betSelection.BetId,
                BetSelectionId = betSelection.BetSelectionId,
                UserId = Convert.ToInt32(betSelection.UserId),
                Username = agent.Username,
                UserTypeId = (int)Shared.Enums.UserType.Player,
                SportId = betSelection.SportId,
                SportName = betSelection.SportName,
                SegmentId = betSelection.SegmentId,
                SegmentName = betSelection.SegmentName,
                LeagueId = betSelection.LeagueId,
                LeagueName = betSelection.LeagueName,
                FixtureId = betSelection.FixtureId,
                FixtureName = betSelection.FixtureName,
                MarketId = betSelection.MarketId,
                MarketName = betSelection.MarketName,
                SelectionId = betSelection.SelectionId,
                SelectionName = betSelection.SelectionName,
                FixtureStatusId = betSelection.FixtureStatusId,
                FixtureStatusName = betSelection.FixtureStatusName,
                Line = betSelection.Line,
                Odds = betSelection.Odds,
                StatusId = betSelection.BetSelectionStatusId,
                StatusName = betSelection.BetSelectionStatusName,
                IsSettled = betSelection.IsSettled,
                IsResettled = betSelection.IsResettled,
                FixtureStartDate = betSelection.FixtureStartDate,
                Score = betSelection.BetScore,
                MarketTemplateGroupId = string.Empty,
                MarketTemplateId = betSelection.MarketTemplateId,
                SelectionTemplateId = betSelection.SelectionTemplateId,
                IsBinary = betSelection.IsBinary
            };
        }
        private void AddOrUpdateRunningBalanceFinal(AgentReportsContext context, decimal trueStake, string currencyCode, Guid betId, int betStatusId, string betStatusName, DateTime betDate, AgentSettings agent, decimal winLossCommission, List<AgentSettings> agents)
        {
            if (betStatusId != (int)BetStatus.Running) return;
            if (agent.UserType == Shared.Enums.UserType.Company) return;

            var runningBalanceEntity = new RunningBalance();

            var runningBalance = context.RunningBalance.Find(agent.UplineUserId, agent.AgentId);

            var uplinePT = agents.Where(c => c.AgentId == agent.UplineUserId).Select(s => s.PositionTaking).FirstOrDefault();

            var preCashBalance = 0m;
            var preTrueCashBalance = 0m;
            var preOutstandingBalance = 0m;

            var cashBalance = 0m;
            var trueCashBalance = 0m;
            var outstandingBalance = 0m;

            if (runningBalance == null)
            {
                outstandingBalance = agent.UserType == Shared.Enums.UserType.Player ? trueStake : trueStake * uplinePT;

                runningBalanceEntity = context.RunningBalance.Add(new RunningBalance
                {
                    ParentUserId = agent.UplineUserId,
                    CurrencyCode = currencyCode,
                    UserId = agent.AgentId,
                    Username = agent.AgentUsername,
                    UserTypeId = (int)agent.UserType,
                    CashBalance = 0m,
                    TrueCashBalance = 0m,
                    OutstandingBalance = outstandingBalance,
                    IsCashPlayer = agent.IsCashMarket
                }).Entity;
            }
            else
            {
                preCashBalance = runningBalance.CashBalance;
                preTrueCashBalance = runningBalance.TrueCashBalance;
                preOutstandingBalance = runningBalance.OutstandingBalance;

                var stakeDeducted = 0m;

                if (agent.UserType == Shared.Enums.UserType.Player && agent.IsCashMarket == false)
                {
                    if ((runningBalance.TrueCashBalance * -1) > 0)
                    {
                        var stake = trueStake * agent.PositionTaking;

                        if ((runningBalance.TrueCashBalance * -1) >= stake)
                            stakeDeducted = stake;
                        else
                            stakeDeducted = Math.Abs(runningBalance.TrueCashBalance);

                        context.CashBalanceTracker.Add(new CashBalanceTracker
                        {
                            ParentUserId = agent.UplineUserId,
                            UserId = agent.AgentId,
                            BetId = betId,
                            UserTypeId = (int)agent.UserType,
                            Amount = stakeDeducted,
                            StatusId = betStatusId,
                            StatusName = betStatusName,
                            TransactionDate = betDate
                        });

                        runningBalance.TrueCashBalance += stakeDeducted;

                        trueCashBalance = stakeDeducted;
                    }
                }

                outstandingBalance = agent.UserType == Shared.Enums.UserType.Player ? trueStake : trueStake * uplinePT;
                runningBalance.OutstandingBalance += outstandingBalance;

                runningBalanceEntity = context.RunningBalance.Update(runningBalance).Entity;
            }

            context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
            {
                ParentUserId = runningBalanceEntity.ParentUserId,
                UserId = runningBalanceEntity.UserId,
                Username = runningBalanceEntity.Username,
                UserTypeId = runningBalanceEntity.UserTypeId,
                CurrencyCode = runningBalanceEntity.CurrencyCode,

                PreCashBalance = preCashBalance,
                CashBalance = cashBalance,
                PostCashBalance = runningBalanceEntity.CashBalance,

                PreTrueCashBalance = preTrueCashBalance,
                TrueCashBalance = trueCashBalance,
                PostTrueCashBalance = runningBalanceEntity.TrueCashBalance,

                PreOutstandingBalance = preOutstandingBalance,
                OutstandingBalance = outstandingBalance,
                PostOutstandingBalance = runningBalanceEntity.OutstandingBalance,

                IsCashPlayer = runningBalanceEntity.IsCashPlayer,
                ReferenceId = betId.ToString().ToUpper(),
                StatusId = betStatusId,
                StatusName = betStatusName,
                TransactionDate = betDate
            });
        }


        private List<AgentSettings> GetAgentSettingsFinal(OnBetSettlement message)
        {
            var agents = new List<AgentSettings>();
            var categoryId = 0;
            var subCategoryId = 0;
            var betSelectionCount = 0;

            if (message.BetSelections.Count == 1)
            {
                if (message.BetSelections[0].SportId == 1)
                {
                    categoryId = (int)Category.Soccer;
                    subCategoryId = message.BetSelections[0].FixtureStatusId == (int)FixtureStatus.Live ? 2 : 1;
                    betSelectionCount = message.BetSelections.Count;
                }
                else if (message.BetSelections[0].SportId == 21)
                {
                    categoryId = (int)Category.Cricket;
                    subCategoryId = message.BetSelections[0].FixtureStatusId == (int)FixtureStatus.Live ? 2 : 1;
                    betSelectionCount = message.BetSelections.Count;
                }
                else
                {
                    categoryId = (int)Category.Sports;
                    subCategoryId = 0;
                    betSelectionCount = message.BetSelections.Count;
                }
            }

            if (message.BetSelections.Count > 1)
            {
                categoryId = (int)Category.Parlay;
                subCategoryId = 0;
                betSelectionCount = message.BetSelections.Count;
            }

            var url = $"{_agentUrl}/api/v3/ptc2/{message.Bet.UserId}/{categoryId}/{subCategoryId}/{betSelectionCount}?transactionDate={message.Bet.BetDate.ToString("s")}";
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(url);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = httpClient.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    var resultList = System.Text.Json.JsonSerializer.Deserialize<BaseResponse<List<AgentSettingsResponse>>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    foreach (var result in resultList.Result)
                    {
                        agents.Add(new AgentSettings
                        {
                            AgentId = result.AgentId,
                            AgentUsername = result.AgentUsername,
                            Username = result.Username,
                            PositionTaking = result.PositionTaking,
                            Commission = result.Commission,
                            UserType = GetUserType(result.UserType),
                            UplineUserId = result.UplineUserId,
                            IsCashMarket = result.IsCashMarket,
                            CurrencyCode = result.CurrencyCode,
                            Hierarchy = result.Hierarchy
                        });
                    }
                }
            }

            return agents;
        }
        private decimal GetReceivePTFinal(OnBetSettlement message, AgentSettings agent)
        {
            var result = 0m;

            var ptStake = agent.PositionTaking * message.Bet.Stake;
            var ptPayout = agent.PositionTaking * message.Bet.Payout;

            switch ((BetStatus)message.Bet.BetStatusId)
            {
                case BetStatus.Won:
                case BetStatus.HalfWon:
                case BetStatus.HalfLost:
                case BetStatus.CashedOut:
                    if (agent.UserType == Shared.Enums.UserType.Player)
                        result = message.Bet.Payout > message.Bet.Stake ? message.Bet.Payout : message.Bet.Payout * -1;
                    else
                        result = ptPayout > ptStake ? ptPayout * -1 : ptPayout;
                    break;
                case BetStatus.Lost:
                    if (agent.UserType == Shared.Enums.UserType.Player)
                        result = message.Bet.Stake * -1;
                    else
                        result = ptStake;
                    break;
                case BetStatus.Void:
                case BetStatus.Rejected:
                case BetStatus.Cancelled:
                    result = 0m;
                    break;
                default:
                    result = ptStake;
                    break;
            }

            return result;
        }
        private BetTransaction ConvertToBetTransactionEntityInsertFinal(OnBetSettlement message, AgentSettings agent)
        {
            return new BetTransaction
            {
                ParentUserId = agent.AgentId,
                BetId = message.Bet.BetId,
                UserId = Convert.ToInt32(message.Bet.UserId),
                Username = agent.Username,
                UserTypeId = (int)Shared.Enums.UserType.Player,
                PositionTaking = agent.PositionTaking,
                Commission = agent.Commission,
                Stake = message.Bet.Stake,
                TrueStake = message.Bet.TrueStake,
                Odds = message.Bet.Odds,
                OddsTypeId = message.Bet.OddsTypeId,
                OddsTypeName = message.Bet.OddsTypeName,
                Payout = message.Bet.Payout,
                WinLoss = message.Bet.WinLoss,
                StatusId = message.Bet.BetStatusId,
                StatusName = message.Bet.BetStatusName,
                Reason = message.Bet.Reason,
                CurrencyCode = message.Bet.CurrencyCode,
                IPAddress = message.Bet.IPAddress,
                Count = message.BetSelections.Count,
                ExchangeRate = message.Bet.ExchangeRate,
                TransactionDate = message.Bet.BetDate,
                TransactionDateOffset = message.Bet.BetDate.AddHours(-4)
            };
        }
        private void AddOrUpdateWinLossFinal(AgentReportsContext context, OnBetSettlement message, AgentSettings agent, List<AgentSettings> agents, Shared.Data.Reports.Entities.Bet betEntity)
        {
            if (message.Bet.BetStatusId == (int)BetStatus.Running) return;

            var dateNow = DateTime.UtcNow;

            agents = agents.OrderBy(o => o.Hierarchy).ToList();

            var receivableComm = agent.Commission;
            var payableComm = agents.Where(c => c.AgentId > agent.AgentId).Select(s => s.Commission).FirstOrDefault();

            var uplineReceivablePT = agents.Where(c => c.AgentId < agent.AgentId).Sum(s => s.PositionTaking);
            var uplinePayablePT = agents.Where(c => c.AgentId <= agent.AgentId).Sum(s => s.PositionTaking);

            var ptReceivableComm = message.Bet.Stake * uplineReceivablePT * receivableComm;
            var ptPayableComm = message.Bet.Stake * uplinePayablePT * payableComm;

            var commission = ptReceivableComm - ptPayableComm;

            var hasHalfSettlement = HasHalfSettlementFinal(message.BetSelections);

            var memberWinLoss = message.Bet.WinLoss;
            var memberCommission = hasHalfSettlement ? (message.Bet.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault()) / 2
                                                      : message.Bet.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault();
            var memberWLC = memberWinLoss + memberCommission;

            var agentWinLoss = Shared.Enums.UserType.Player == agent.UserType ? agent.PositionTaking * message.Bet.WinLoss : agent.PositionTaking * message.Bet.WinLoss * -1;
            var agentCommission = hasHalfSettlement ? commission / 2 : commission;
            var agentWLC = agentWinLoss + agentCommission;

            var res = GetUplineAgentWinLossCommission2Final(message.Bet.Stake, hasHalfSettlement, agent, agents, memberWinLoss * -1);

            AddOrUpdateSettledBalanceFinal(context, message, agent, res.Item1 + res.Item2, agents);

            var tempDownlineWinLoss = 0m;
            var tempDownlineCommission = 0m;
            var tempDownlineWLC = 0m;

            var isGGREnabled = false;
            var agentId = agents.FirstOrDefault(a => a.Hierarchy == 1)?.AgentId ?? 0;
            var partnerRevShare = context.PartnerRevenueShare.FirstOrDefault(p => p.UserId == agentId);
            if (partnerRevShare != null)
                isGGREnabled = partnerRevShare.IsGGREnabled;

            foreach (var data in agents.Where(c => c.AgentId > agent.AgentId).ToList())
            {
                var parentUserId = agent.AgentId;
                var directUserId = data.UplineUserId;
                var indirectUserId = data.AgentId;

                memberWinLoss = message.Bet.WinLoss;
                memberCommission = hasHalfSettlement ? (message.Bet.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault()) / 2
                                                          : message.Bet.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault();
                memberWLC = memberWinLoss + memberCommission;

                agentWinLoss = agent.PositionTaking * memberWinLoss * -1;
                agentCommission = hasHalfSettlement ? commission / 2 : commission;
                agentWLC = agentWinLoss + agentCommission;

                var upline = GetUplineAgentWinLossCommissionFinal(message.Bet.Stake, hasHalfSettlement, agent, agents, memberWinLoss * -1);

                var uplineWinLoss = upline.Item1;
                var uplineCommission = upline.Item2;
                var uplineWLC = uplineWinLoss + uplineCommission;

                var downline = GetUplineAgentWinLossCommissionFinal(message.Bet.Stake, hasHalfSettlement, agent, agents, memberWinLoss * -1);

                var downlineWinLoss = downline.Item1;
                var downlineCommission = downline.Item2;
                var downlineWLC = downlineWinLoss + downlineCommission;

                if (data.UserType == Shared.Enums.UserType.Agent)
                {
                    tempDownlineWinLoss = downlineWinLoss;
                    tempDownlineCommission = downlineCommission;
                    tempDownlineWLC = downlineWLC;
                }

                if (data.UserType == Shared.Enums.UserType.Player)
                {
                    downlineWinLoss = tempDownlineWinLoss;
                    downlineCommission = tempDownlineCommission;
                    downlineWLC = tempDownlineWLC;
                }

                var winloss = context.WinLoss.Where(x => x.ParentUserId == parentUserId && x.DirectUserId == directUserId
                && x.IndirectUserId == indirectUserId && x.BetId == betEntity.BetId && x.TransactionDate == betEntity.TransactionDate).SingleOrDefault();

                if (winloss != null)
                {
                    winloss.ExchangeRate = betEntity.ExchangeRate;
                    winloss.AgentUsername = agents.Where(c => c.AgentId == data.UplineUserId).Select(s => s.AgentUsername).FirstOrDefault();
                    winloss.Username = data.UserType == Shared.Enums.UserType.Player ? data.Username : data.AgentUsername;
                    winloss.UserTypeId = (int)agents.Where(c => c.AgentId == indirectUserId).Select(c => c.UserType).FirstOrDefault();
                    winloss.CurrencyCode = betEntity.CurrencyCode;
                    winloss.Commission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission;
                    winloss.MemberWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberWinLoss;
                    winloss.MemberCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission;
                    winloss.MemberWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberWLC;
                    winloss.AgentWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentWinLoss;
                    winloss.AgentCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentCommission;
                    winloss.AgentWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentWLC;
                    winloss.UplineWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineWinLoss;
                    winloss.UplineCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineCommission;
                    winloss.UplineWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineWLC;
                    winloss.DownlineWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineWinLoss;
                    winloss.DownlineCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineCommission;
                    winloss.DownlineWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineWLC;
                    winloss.IsParlay = betEntity.Count > 1;
                    winloss.Stake = betEntity.Stake;
                    winloss.TrueStake = message.Bet.BetStatusId == (int)BetStatus.Cancelled ? 0 : betEntity.TrueStake;
                    winloss.Odds = betEntity.Odds;
                    winloss.OddsTypeId = betEntity.OddsTypeId;
                    winloss.OddsTypeName = betEntity.OddsTypeName;
                    winloss.StatusId = betEntity.StatusId;
                    winloss.StatusName = betEntity.StatusName;
                    winloss.SportId = betEntity.SportId;
                    winloss.SportName = betEntity.SportName;
                    winloss.SegmentId = betEntity.SegmentId;
                    winloss.SegmentName = betEntity.SegmentName;
                    winloss.LeagueId = betEntity.LeagueId;
                    winloss.LeagueName = betEntity.LeagueName;
                    winloss.FixtureId = betEntity.FixtureId;
                    winloss.FixtureName = betEntity.FixtureName;
                    winloss.FixtureStartDate = betEntity.FixtureStartDate;
                    winloss.FixtureStatusId = betEntity.FixtureStatusId;
                    winloss.FixtureStatusName = betEntity.FixtureStatusName;
                    winloss.Score = betEntity.Score;
                    winloss.MarketTemplateGroupId = betEntity.MarketTemplateGroupId;
                    winloss.MarketTemplateGroupName = betEntity.MarketTemplateGroupName;
                    winloss.MarketTemplateId = betEntity.MarketTemplateId;
                    winloss.MarketId = betEntity.MarketId;
                    winloss.MarketName = betEntity.MarketName;
                    winloss.Line = betEntity.Line;
                    winloss.SelectionTemplateId = betEntity.SelectionTemplateId;
                    winloss.SelectionId = betEntity.SelectionId;
                    winloss.SelectionName = betEntity.SelectionName;
                    winloss.SettledDate = dateNow;
                    winloss.SettledDateOffset = dateNow.AddHours(-4);
                    winloss.ProductId = (int)Shared.Enums.Product.Sportsbook;
                    winloss.ProductName = Shared.Enums.Product.Sportsbook.ToString();
                    winloss.PositionTakingDetails = JsonConvert.SerializeObject(GetPTFinal(agent, agents));
                    winloss.CommissionDetails = JsonConvert.SerializeObject(agents.Where(c => c.AgentId >= agent.AgentId).Select(s => new { Username = s.AgentUsername, Commission = s.Commission * 100 }).ToList());
                    winloss.MarketTypeId = string.Empty;
                    winloss.MarketGroupId = string.Empty;
                    winloss.MaxLiability = 0;
                    winloss.IsGGREnabled = isGGREnabled;
                    winloss.Payout = message.Bet.BetStatusId == (int)BetStatus.Cancelled ? 0 : message.Bet.Payout;
                    winloss.GGR = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : (message.Bet.TrueStake - message.Bet.Payout);
                }
                else
                    context.WinLoss.Add(new WinLoss
                    {
                        ParentUserId = parentUserId,
                        DirectUserId = directUserId,
                        IndirectUserId = indirectUserId,
                        BetId = betEntity.BetId,
                        TransactionDate = betEntity.TransactionDate,
                        TransactionDateOffset = betEntity.TransactionDate.AddHours(-4),
                        ExchangeRate = betEntity.ExchangeRate,
                        AgentUsername = agents.Where(c => c.AgentId == data.UplineUserId).Select(s => s.AgentUsername).FirstOrDefault(),
                        Username = data.UserType == Shared.Enums.UserType.Player ? data.Username : data.AgentUsername,
                        UserTypeId = (int)agents.Where(c => c.AgentId == indirectUserId).Select(c => c.UserType).FirstOrDefault(),
                        CurrencyCode = betEntity.CurrencyCode,
                        Commission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission,
                        MemberWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberWinLoss,
                        MemberCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission,
                        MemberWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : memberWLC,
                        AgentWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentWinLoss,
                        AgentCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentCommission,
                        AgentWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : agentWLC,
                        UplineWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineWinLoss,
                        UplineCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineCommission,
                        UplineWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : uplineWLC,
                        DownlineWinLoss = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineWinLoss,
                        DownlineCommission = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineCommission,
                        DownlineWLC = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : downlineWLC,
                        IsParlay = betEntity.Count > 1 ? true : false,
                        Stake = betEntity.Stake,
                        TrueStake = message.Bet.BetStatusId == (int)BetStatus.Cancelled ? 0 : betEntity.TrueStake,
                        Odds = betEntity.Odds,
                        OddsTypeId = betEntity.OddsTypeId,
                        OddsTypeName = betEntity.OddsTypeName,
                        StatusId = betEntity.StatusId,
                        StatusName = betEntity.StatusName,
                        SportId = betEntity.SportId,
                        SportName = betEntity.SportName,
                        SegmentId = betEntity.SegmentId,
                        SegmentName = betEntity.SegmentName,
                        LeagueId = betEntity.LeagueId,
                        LeagueName = betEntity.LeagueName,
                        FixtureId = betEntity.FixtureId,
                        FixtureName = betEntity.FixtureName,
                        FixtureStartDate = betEntity.FixtureStartDate,
                        FixtureStatusId = betEntity.FixtureStatusId,
                        FixtureStatusName = betEntity.FixtureStatusName,
                        Score = betEntity.Score,
                        MarketTemplateGroupId = string.Empty,
                        MarketTemplateGroupName = string.Empty,
                        MarketTemplateId = betEntity.MarketTemplateId,
                        MarketId = betEntity.MarketId,
                        MarketName = betEntity.MarketName,
                        Line = betEntity.Line,
                        SelectionTemplateId = betEntity.SelectionTemplateId,
                        SelectionId = betEntity.SelectionId,
                        SelectionName = betEntity.SelectionName,
                        SettledDate = dateNow,
                        SettledDateOffset = dateNow.AddHours(-4),
                        ProductId = (int)Shared.Enums.Product.Sportsbook,
                        ProductName = Shared.Enums.Product.Sportsbook.ToString(),
                        CasinoDetails = string.Empty,
                        GameProvider = 0,
                        PositionTakingDetails = JsonConvert.SerializeObject(GetPTFinal(agent, agents)),
                        CommissionDetails = JsonConvert.SerializeObject(agents.Where(c => c.AgentId >= agent.AgentId).Select(s => new { Username = s.AgentUsername, Commission = s.Commission * 100 }).ToList()),
                        ProviderBrandId = 0,
                        ProviderBrandName = string.Empty,
                        GameId = 0,
                        GameName = string.Empty,
                        ReferenceId = string.Empty,
                        MarketTypeId = string.Empty,
                        MarketGroupId = string.Empty,
                        MaxLiability = 0,
                        IsGGREnabled = isGGREnabled,
                        Payout = message.Bet.BetStatusId == (int)BetStatus.Cancelled ? 0 : message.Bet.Payout,
                        GGR = message.Bet.BetStatusId == (int)BetStatus.Cancelled || message.Bet.BetStatusId == (int)BetStatus.Void ? 0 : (message.Bet.TrueStake - message.Bet.Payout)
                    });
            }
        }

        private void AddOrUpdateCasinoWinLossFinal(AgentReportsContext context, OnCasinoBetSettlement message, AgentSettings agent, List<AgentSettings> agents, ProductLookup product)
        {
            if (message.BetStatusId == (int)BetStatus.Running) return;

            var dateNow = DateTime.UtcNow;

            agents = agents.OrderBy(o => o.Hierarchy).ToList();

            var receivableComm = agent.Commission;
            var payableComm = agents.Where(c => c.AgentId > agent.AgentId).Select(s => s.Commission).FirstOrDefault();

            var uplineReceivablePT = agents.Where(c => c.AgentId < agent.AgentId).Sum(s => s.PositionTaking);
            var uplinePayablePT = agents.Where(c => c.AgentId <= agent.AgentId).Sum(s => s.PositionTaking);

            var ptReceivableComm = message.Stake * uplineReceivablePT * receivableComm;
            var ptPayableComm = message.Stake * uplinePayablePT * payableComm;

            var commission = ptReceivableComm - ptPayableComm;

            var memberWinLoss = message.WinLoss;
            var memberCommission = message.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault();
            var memberWLC = memberWinLoss + memberCommission;

            var agentWinLoss = Shared.Enums.UserType.Player == agent.UserType ? agent.PositionTaking * message.WinLoss : agent.PositionTaking * message.WinLoss * -1;
            var agentCommission = commission;
            var agentWLC = agentWinLoss + agentCommission;

            var res = GetUplineAgentWinLossCommission2Final(message.Stake, false, agent, agents, memberWinLoss * -1);

            AddOrUpdateCasinoSettledBalanceFinal(context, message, agent, res.Item1 + res.Item2, agents);

            var tempDownlineWinLoss = 0m;
            var tempDownlineCommission = 0m;
            var tempDownlineWLC = 0m;

            var isGGREnabled = false;
            var agentId = agents.FirstOrDefault(a => a.Hierarchy == 1)?.AgentId ?? 0;
            var partnerRevShare = context.PartnerRevenueShare.FirstOrDefault(p => p.UserId == agentId);
            if (partnerRevShare != null)
                isGGREnabled = partnerRevShare.IsGGREnabled;

            foreach (var data in agents.Where(c => c.AgentId > agent.AgentId).ToList())
            {
                var parentUserId = agent.AgentId;
                var directUserId = data.UplineUserId;
                var indirectUserId = data.AgentId;

                memberWinLoss = message.WinLoss;
                memberCommission = message.Stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Select(s => s.Commission).FirstOrDefault();
                memberWLC = memberWinLoss + memberCommission;

                agentWinLoss = agent.PositionTaking * memberWinLoss * -1;
                agentCommission = commission;
                agentWLC = agentWinLoss + agentCommission;

                var upline = GetUplineAgentWinLossCommissionFinal(message.Stake, false, agent, agents, memberWinLoss * -1);

                var uplineWinLoss = upline.Item1;
                var uplineCommission = upline.Item2;
                var uplineWLC = uplineWinLoss + uplineCommission;

                var downline = GetUplineAgentWinLossCommissionFinal(message.Stake, false, agent, agents, memberWinLoss * -1);

                var downlineWinLoss = downline.Item1;
                var downlineCommission = downline.Item2;
                var downlineWLC = downlineWinLoss + downlineCommission;

                if (data.UserType == Shared.Enums.UserType.Agent)
                {
                    tempDownlineWinLoss = downlineWinLoss;
                    tempDownlineCommission = downlineCommission;
                    tempDownlineWLC = downlineWLC;
                }

                if (data.UserType == Shared.Enums.UserType.Player)
                {
                    downlineWinLoss = tempDownlineWinLoss;
                    downlineCommission = tempDownlineCommission;
                    downlineWLC = tempDownlineWLC;
                }

                var winloss = context.CasinoWinLoss.Where(x => x.ParentUserId == parentUserId && x.DirectUserId == directUserId
                && x.IndirectUserId == indirectUserId && x.BetId == message.BetId && x.BetDate == message.BetDate).SingleOrDefault();

                if (winloss != null)
                {
                    winloss.Reason = message.Reason;
                    winloss.ExchangeRate = message.ExchangeRate;
                    winloss.AgentUsername = agents.Where(c => c.AgentId == data.UplineUserId).Select(s => s.AgentUsername).FirstOrDefault();
                    winloss.UserId = message.UserId;
                    winloss.UserTypeId = (int)agents.Where(c => c.AgentId == indirectUserId).Select(c => c.UserType).FirstOrDefault();
                    winloss.TransactionType = message.TransactionType;
                    winloss.Commission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission;
                    winloss.MemberWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberWinLoss;
                    winloss.MemberCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission;
                    winloss.MemberWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberWLC;
                    winloss.AgentWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentWinLoss;
                    winloss.AgentCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentCommission;
                    winloss.AgentWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentWLC;
                    winloss.UplineWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineWinLoss;
                    winloss.UplineCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineCommission;
                    winloss.UplineWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineWLC;
                    winloss.DownlineWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineWinLoss;
                    winloss.DownlineCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineCommission;
                    winloss.DownlineWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineWLC;
                    winloss.Stake = message.Stake;
                    winloss.BetStatusId = message.BetStatusId;
                    winloss.BetStatusName = message.BetStatusName;
                    winloss.CurrencyCode = message.CurrencyCode;
                    winloss.GameId = message.GameId;
                    winloss.Device = message.Device;
                    winloss.ClientType = message.ClientType;
                    winloss.ProviderBetId = message.ProviderBetId;
                    winloss.Category = message.Category;
                    winloss.IsComplete = message.IsComplete;
                    winloss.ReceivePositionTaking = GetReceivePTFinal(message.Stake, message.Payout, message.BetStatusId, agent);
                    winloss.JackpotContributions = message.JackpotContributions;
                    winloss.ConversionRate = message.ConversionRate;
                    winloss.BonusType = message.BonusType;
                    winloss.BonusPromoCode = message.BonusPromoCode;
                    winloss.JackpotPayout = message.JackpotPayout;
                    winloss.TableId = message.TableId;
                    winloss.TransactionIds = message.TransactionIds;
                    winloss.ProviderTransactionDate = message.ProviderTransactionDate;
                    winloss.SettledDate = dateNow;
                    winloss.MaxLiability = 0;
                    winloss.GGR = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : (message.Stake - message.Payout);
                    winloss.Payout = message.BetStatusId == (int)BetStatus.Cancelled ? 0 : message.Payout;
                    winloss.IsGGREnabled = isGGREnabled;
                    winloss.PositionTakingDetails = JsonConvert.SerializeObject(GetPTFinal(agent, agents));
                    winloss.CasinoDetails = JsonConvert.SerializeObject(GetCasinoDetails(message, product));
                }
                else
                    context.CasinoWinLoss.Add(new CasinoWinLoss
                    {
                        ParentUserId = parentUserId,
                        DirectUserId = directUserId,
                        IndirectUserId = indirectUserId,
                        BetId = message.BetId,
                        BetDate = message.BetDate,
                        TransactionDateOffset = message.BetDate.AddHours(-4),
                        ProviderTransactionId = message.ProviderTransactionId,
                        ExchangeRate = message.ExchangeRate,
                        AgentId = agent.AgentId,
                        AgentUsername = agents.Where(c => c.AgentId == data.UplineUserId).Select(s => s.AgentUsername).FirstOrDefault(),
                        ClientId = message.ClientId,
                        ClientCode = message.ClientCode,
                        ClientName = message.ClientName,
                        Endpoint = message.Endpoint,
                        OperatorToken = message.OperatorToken,
                        Reason = message.Reason,
                        IsAgent = message.IsAgent,
                        StakeBonus = message.StakeBonus,
                        UserTypeId = (int)agents.Where(c => c.AgentId == indirectUserId).Select(c => c.UserType).FirstOrDefault(),
                        PositionTaking = GetReceivePTFinal(message.Stake, message.Payout, message.BetStatusId, agent),
                        UserId = message.UserId,
                        Username = data.UserType == Shared.Enums.UserType.Player ? data.Username : data.AgentUsername,
                        TransactionType = message.TransactionType,
                        Commission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission,
                        MemberWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberWinLoss,
                        MemberCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberCommission,
                        MemberWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : memberWLC,
                        AgentWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentWinLoss,
                        AgentCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentCommission,
                        AgentWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : agentWLC,
                        UplineWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineWinLoss,
                        UplineCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineCommission,
                        UplineWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : uplineWLC,
                        DownlineWinLoss = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineWinLoss,
                        DownlineCommission = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineCommission,
                        DownlineWLC = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : downlineWLC,
                        Stake = message.Stake,
                        BetStatusId = message.BetStatusId,
                        BetStatusName = message.BetStatusName,
                        CurrencyCode = message.CurrencyCode,
                        GameId = message.GameId,
                        Device = message.Device,
                        ClientType = message.ClientType,
                        ProviderBetId = message.ProviderBetId,
                        Category = message.Category,
                        IsComplete = message.IsComplete,
                        ReceivePositionTaking = GetReceivePTFinal(message.Stake, message.Payout, message.BetStatusId, agent),
                        JackpotContributions = message.JackpotContributions,
                        ConversionRate = message.ConversionRate,
                        BonusType = message.BonusType,
                        BonusPromoCode = message.BonusPromoCode,
                        JackpotPayout = message.JackpotPayout,
                        TableId = message.TableId,
                        TransactionIds = message.TransactionIds,
                        ProviderTransactionDate = message.ProviderTransactionDate,
                        SettledDate = dateNow,
                        MaxLiability = 0,
                        GGR = message.BetStatusId == (int)BetStatus.Cancelled || message.BetStatusId == (int)BetStatus.Void ? 0 : (message.Stake - message.Payout),
                        Payout = message.BetStatusId == (int)BetStatus.Cancelled ? 0 : message.Payout,
                        IsGGREnabled = isGGREnabled,
                        ProductTypeId = product.ProductTypeId,
                        ProductTypeName = product.ProductTypeName,
                        GameType = product.GameType,
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        ProviderName = product.ProviderName,
                        PositionTakingDetails = JsonConvert.SerializeObject(GetPTFinal(agent, agents)),
                        CasinoDetails = JsonConvert.SerializeObject(GetCasinoDetails(message, product))
            });
            }
        }

        private ProductLookup UpdateProductLookup(AgentReportsContext context, string category, string gameId)
        {
            var categoryType = GetProduct(category);
            var gameType = category.Replace("CASINO/LIVECASINO/","").Replace("CASINO/VIRTUAL_SPORTS/", "").Replace("CASINO/", "");
            var provider = !string.IsNullOrEmpty(gameId) ? gameId.Split("-").FirstOrDefault() : string.Empty;
            var productName = !string.IsNullOrEmpty(gameId) ? gameId.Split("-").LastOrDefault() : string.Empty;

            var product = context.ProductLookup.FirstOrDefault(p => p.ProductTypeId == (int)categoryType && p.GameType == gameType && p.ProviderName == provider && p.ProductName == productName);
            if (product == null)
                product = context.ProductLookup.Add(new ProductLookup
                {
                    ProductTypeId = (int)categoryType,
                    ProductTypeName = categoryType.ToString(),
                    GameType = gameType,
                    ProviderName = provider,
                    ProductName = productName
                }).Entity;

            return product;
        }

        private bool HasHalfSettlementFinal(List<Shared.Data.Betting.Entities.BetSelection> selections)
        {
            var halfSettlements = new List<int> { (int)BetStatus.HalfWon, (int)BetStatus.HalfLost };

            var count = selections.Where(c => halfSettlements.Contains(c.BetSelectionStatusId)).Count();

            return count == 0 ? false : true;
        }

        private Tuple<decimal, decimal> GetUplineAgentWinLossCommission2Final(decimal stake, bool hasHalfSettlement, AgentSettings agent, List<AgentSettings> agents, decimal playerWinLoss)
        {
            var filtered = agents.Where(c => c.UserType != Shared.Enums.UserType.Player && c.AgentId < agent.AgentId).ToList();

            var winLoss = 0m;
            var commission = 0m;

            foreach (var item in filtered)
            {
                winLoss += GetAgentWinLoss(playerWinLoss, item);
                commission += GetReceiveCommissionFinal(stake, hasHalfSettlement, item, agents);
            }

            return new Tuple<decimal, decimal>(winLoss, commission);
        }
        private decimal GetReceiveCommissionFinal(decimal stake, bool hasHalfSettlement, AgentSettings agent, List<AgentSettings> agents)
        {
            if (agent.UserType == Shared.Enums.UserType.Player)
            {
                var playerCommission = stake * agents.Where(c => c.UserType == Shared.Enums.UserType.Player).Sum(s => s.Commission);

                return hasHalfSettlement ? playerCommission / 2 : playerCommission;
            }
            else

                return GetAgentReceiveCommissionFinal(stake, hasHalfSettlement, agent, agents);
        }

        private decimal GetAgentReceiveCommissionFinal(decimal stake, bool hasHalfSettlement, AgentSettings agent, List<AgentSettings> agents)
        {
            var receivableComm = agent.Commission;
            var payableComm = agents.OrderBy(o => o.Hierarchy).Where(c => c.AgentId > agent.AgentId).Select(s => s.Commission).FirstOrDefault();

            var uplineReceivablePT = agents.OrderBy(o => o.Hierarchy).Where(c => c.AgentId < agent.AgentId).Sum(s => s.PositionTaking);
            var uplinePayablePT = agents.OrderBy(o => o.Hierarchy).Where(c => c.AgentId <= agent.AgentId).Sum(s => s.PositionTaking);

            var ptReceivableComm = stake * uplineReceivablePT * receivableComm;
            var ptPayableComm = stake * uplinePayablePT * payableComm;

            var commission = ptReceivableComm - ptPayableComm;

            return hasHalfSettlement ? commission / 2 : commission;
        }

        private void AddOrUpdateSettledBalanceFinal(AgentReportsContext context, OnBetSettlement message, AgentSettings agent, decimal winLossCommission, List<AgentSettings> agents)
        {
            if (message.Bet.BetStatusId == (int)BetStatus.Running) return;
            if (agent.UserType == Shared.Enums.UserType.Company) return;

            var dateUTCNow = DateTime.UtcNow;

            var betRecord = context.BetTransaction.Any(c => c.BetId == message.Bet.BetId && c.StatusId == (int)BetStatus.Running);
            var runningBalance = context.RunningBalance.Find(agent.UplineUserId, agent.AgentId);

            var preCashBalance = 0m;
            var preTrueCashBalance = 0m;
            var preOutstandingBalance = 0m;

            var cashBalance = 0m;
            var trueCashBalance = 0m;
            var outstandingBalance = 0m;

            if (runningBalance != null)
            {
                preCashBalance = runningBalance.CashBalance;
                preTrueCashBalance = runningBalance.TrueCashBalance;
                preOutstandingBalance = runningBalance.OutstandingBalance;

                var tracker = context.CashBalanceTracker.Find(agent.UplineUserId, agent.AgentId, message.Bet.BetId);

                var amount = 0m;

                if (tracker != null)
                {
                    amount = tracker.Amount;

                    tracker.StatusId = message.Bet.BetStatusId;
                    tracker.StatusName = message.Bet.BetStatusName;

                    context.CashBalanceTracker.Update(tracker);
                }

                var uplinePT = agents.Where(c => c.AgentId == agent.UplineUserId).Select(s => s.PositionTaking).FirstOrDefault();

                var BetStatuss = new List<BetStatus> { BetStatus.Void, BetStatus.Rejected };

                if (BetStatuss.Contains((BetStatus)message.Bet.BetStatusId))
                {
                    outstandingBalance = betRecord ? agent.UserType == Shared.Enums.UserType.Player ? message.Bet.TrueStake : message.Bet.TrueStake * uplinePT
                                                   : 0;
                    runningBalance.OutstandingBalance -= outstandingBalance;

                    trueCashBalance = betRecord ? amount : 0;

                    runningBalance.TrueCashBalance -= trueCashBalance;
                }
                else
                {
                    cashBalance = winLossCommission;

                    trueCashBalance = betRecord ? winLossCommission - amount : 0;
                    outstandingBalance = betRecord ? agent.UserType == Shared.Enums.UserType.Player ? message.Bet.TrueStake : message.Bet.TrueStake * uplinePT
                                                   : 0;

                    if (agent.IsCashMarket == false)
                    {
                        runningBalance.CashBalance += cashBalance;
                        runningBalance.TrueCashBalance += trueCashBalance;
                    }
                    else
                    {
                        var cashBalanceTotal = runningBalance.CashBalance + cashBalance;

                        if (cashBalanceTotal > 0)
                        {
                            var tempCashBalance = runningBalance.CashBalance;

                            runningBalance.CashBalance -= tempCashBalance;
                            runningBalance.TrueCashBalance -= tempCashBalance;

                            cashBalance = tempCashBalance;
                            trueCashBalance = tempCashBalance;
                        }
                        else
                        {
                            runningBalance.CashBalance += cashBalance;
                            runningBalance.TrueCashBalance += trueCashBalance;
                        }
                    }

                    runningBalance.OutstandingBalance -= outstandingBalance;
                }

                var entity = context.RunningBalance.Update(runningBalance).Entity;

                context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
                {
                    ParentUserId = entity.ParentUserId,
                    UserId = entity.UserId,
                    Username = entity.Username,
                    UserTypeId = entity.UserTypeId,
                    CurrencyCode = entity.CurrencyCode,

                    PreCashBalance = preCashBalance,
                    CashBalance = cashBalance,
                    PostCashBalance = entity.CashBalance,

                    PreTrueCashBalance = preTrueCashBalance,
                    TrueCashBalance = trueCashBalance,
                    PostTrueCashBalance = entity.TrueCashBalance,

                    PreOutstandingBalance = preOutstandingBalance,
                    OutstandingBalance = outstandingBalance,
                    PostOutstandingBalance = entity.OutstandingBalance,

                    IsCashPlayer = entity.IsCashPlayer,
                    ReferenceId = message.Bet.BetId.ToString().ToUpper(),
                    StatusId = message.Bet.BetStatusId,
                    StatusName = message.Bet.BetStatusName,
                    TransactionDate = DateTime.UtcNow
                });
            }
        }
        private void AddOrUpdateCasinoSettledBalanceFinal(AgentReportsContext context, OnCasinoBetSettlement message, AgentSettings agent, decimal winLossCommission, List<AgentSettings> agents)
        {
            if (message.BetStatusId == (int)BetStatus.Running) return;
            if (agent.UserType == Shared.Enums.UserType.Company) return;

            var dateUTCNow = DateTime.UtcNow;

            var runningBalance = context.RunningBalance.Find(agent.UplineUserId, agent.AgentId);

            var preCashBalance = 0m;
            var preTrueCashBalance = 0m;
            var preOutstandingBalance = 0m;

            var cashBalance = 0m;
            var trueCashBalance = 0m;
            var outstandingBalance = 0m;

            if (runningBalance != null)
            {
                preCashBalance = runningBalance.CashBalance;
                preTrueCashBalance = runningBalance.TrueCashBalance;
                preOutstandingBalance = runningBalance.OutstandingBalance;

                var tracker = context.CashBalanceTracker.Find(agent.UplineUserId, agent.AgentId, message.BetId);

                var amount = 0m;

                if (tracker != null)
                {
                    amount = tracker.Amount;

                    tracker.StatusId = message.BetStatusId;
                    tracker.StatusName = message.BetStatusName;

                    context.CashBalanceTracker.Update(tracker);
                }

                var uplinePT = agents.Where(c => c.AgentId == agent.UplineUserId).Select(s => s.PositionTaking).FirstOrDefault();

                var BetStatuss = new List<BetStatus> { BetStatus.Void, BetStatus.Rejected };

                if (BetStatuss.Contains((BetStatus)message.BetStatusId))
                {
                    outstandingBalance = 0;
                    runningBalance.OutstandingBalance -= outstandingBalance;

                    trueCashBalance = 0;

                    runningBalance.TrueCashBalance -= trueCashBalance;
                }
                else
                {
                    cashBalance = winLossCommission;

                    trueCashBalance = 0;
                    outstandingBalance = 0;

                    if (agent.IsCashMarket == false)
                    {
                        runningBalance.CashBalance += cashBalance;
                        runningBalance.TrueCashBalance += trueCashBalance;
                    }
                    else
                    {
                        var cashBalanceTotal = runningBalance.CashBalance + cashBalance;

                        if (cashBalanceTotal > 0)
                        {
                            var tempCashBalance = runningBalance.CashBalance;

                            runningBalance.CashBalance -= tempCashBalance;
                            runningBalance.TrueCashBalance -= tempCashBalance;

                            cashBalance = tempCashBalance;
                            trueCashBalance = tempCashBalance;
                        }
                        else
                        {
                            runningBalance.CashBalance += cashBalance;
                            runningBalance.TrueCashBalance += trueCashBalance;
                        }
                    }

                    runningBalance.OutstandingBalance -= outstandingBalance;
                }

                var entity = context.RunningBalance.Update(runningBalance).Entity;

                context.RunningBalanceTransaction.Add(new RunningBalanceTransaction
                {
                    ParentUserId = entity.ParentUserId,
                    UserId = entity.UserId,
                    Username = entity.Username,
                    UserTypeId = entity.UserTypeId,
                    CurrencyCode = entity.CurrencyCode,

                    PreCashBalance = preCashBalance,
                    CashBalance = cashBalance,
                    PostCashBalance = entity.CashBalance,

                    PreTrueCashBalance = preTrueCashBalance,
                    TrueCashBalance = trueCashBalance,
                    PostTrueCashBalance = entity.TrueCashBalance,

                    PreOutstandingBalance = preOutstandingBalance,
                    OutstandingBalance = outstandingBalance,
                    PostOutstandingBalance = entity.OutstandingBalance,

                    IsCashPlayer = entity.IsCashPlayer,
                    ReferenceId = message.BetId.ToString().ToUpper(),
                    StatusId = message.BetStatusId,
                    StatusName = message.BetStatusName,
                    TransactionDate = DateTime.UtcNow
                });
            }
        }

        private Tuple<decimal, decimal> GetUplineAgentWinLossCommissionFinal(decimal stake, bool hasHalfSettlement, AgentSettings agent, List<AgentSettings> agents, decimal playerWinLoss)
        {
            var filtered = agents.Where(c => c.UserType != Shared.Enums.UserType.Player && c.AgentId < agent.AgentId).ToList();

            if (filtered.Count == 0 || agent.UserType == Shared.Enums.UserType.Player) return new Tuple<decimal, decimal>(0, 0);

            var winLoss = 0m;
            var commission = 0m;

            foreach (var item in filtered)
            {
                winLoss += GetAgentWinLoss(playerWinLoss, item);
                commission += GetReceiveCommissionFinal(stake, hasHalfSettlement, item, agents);
            }

            return new Tuple<decimal, decimal>(winLoss, commission);
        }
        private object GetPTFinal(AgentSettings agent, List<AgentSettings> agents)
        {
            var uplineResult = agents.Where(c => c.AgentId < agent.AgentId).Sum(s => s.PositionTaking) * 100;
            var selfResult = agents.Where(c => c.AgentId == agent.AgentId).Sum(s => s.PositionTaking) * 100;
            var downlineResult = agents.Where(c => c.AgentId > agent.AgentId && c.UserType != Shared.Enums.UserType.Player).Sum(s => s.PositionTaking) * 100;

            return new
            {
                Upline = uplineResult,
                Agent = selfResult,
                Downline = downlineResult
            };
        }

        private CasinoDetailDTO GetCasinoDetails(OnCasinoBetSettlement message, ProductLookup product)
        {
            return new CasinoDetailDTO
            {
                ProviderTransactionId = message.ProviderBetId,
                ProviderTransactionDate = message.ProviderTransactionDate,
                GameTypeId = string.Empty,
                GameTypeName = product.GameType,
                GameId = product.ProductId,
                GameName = product.ProductName,
                GameTransactionId = message.BetId.ToString(),
                ProviderBrandId = product.ProductTypeId,
                ProviderBrandName = product.ProductTypeName,
                ProviderId = 0,
                ProviderName = product.ProviderName,
                Payout = message.Payout
            };
        }

        private Shared.Enums.UserType GetUserType(int userTypeId)
        {
            switch (userTypeId)
            {
                case 0:
                    return Shared.Enums.UserType.Company;
                case 10:
                    return Shared.Enums.UserType.Agent;
                default:
                    return Shared.Enums.UserType.Player;
            }
        }
        private decimal GetAgentWinLoss(decimal playerWinLoss, AgentSettings agent)
        {
            return agent.PositionTaking * playerWinLoss;
            //switch (agent.UserType)
            //{
            //    case UserType.Company:
            //    case UserType.Agent:
            //        return playerWinLoss > 0 ? agent.PositionTaking * playerWinLoss * -1 : agent.PositionTaking * playerWinLoss;
            //    default:
            //        return agent.PositionTaking * playerWinLoss;
            //}
        }

        private Product GetProduct(string category)
        {
            var newCategory = Product.Casino;

            if (category.ToLower().Contains("virtual"))
            {
                newCategory = Product.VirtualGames;
            }
            else if (category.ToLower().Contains("live"))
            {
                newCategory = Product.LiveCasino;
            }
            return newCategory;
        }

        private Category GetCategory(string category)
        {
            var newCategory = Category.Casino;

            if (category.ToLower().Contains("virtual"))
            {
                newCategory = Category.VirtualGames;
            }
            else if (category.ToLower().Contains("live"))
            {
                newCategory = Category.LiveCasino;
            }
            return newCategory;
        }
    }
}

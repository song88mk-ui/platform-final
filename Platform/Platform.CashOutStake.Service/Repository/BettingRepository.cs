using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Shared.Data.Betting;
using Platform.Shared.Enums;
using Platform.Shared.MessageModels;
using Platform.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace Platform.CashOutStake.Service.Repository
{
    public class BettingRepository : IBettingRepository
    {
        private readonly IServiceProvider _serviceProvider;
        public List<Tuple<long, List<BetMemory>>> _fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BettingRepository(IServiceProvider serviceProvider, HttpClient httpClient, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["API:MTSProbabilityAPI"]);
            _httpClient.DefaultRequestHeaders.Add("x-access-token", "bx8zTQrkEsbP0Gq7CS");
        }

        public void PreloadActiveBets()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BettingContext>();

            var bets = context.Bet.AsNoTracking().Where(c => c.BetStatusId == (int)BetStatus.Running).Select(s => new BetMemory 
            {
                BetId = s.BetId,
                ClientId = s.ClientId,
                UserId = s.UserId,
                BetStatusId = s.BetStatusId,
                TrueStake = s.TrueStake,
                Odds = s.Odds
            }).ToList();

            var betIds = bets.Select(s => s.BetId).ToList();

            var betSelections = context.BetSelection.AsNoTracking().Where(c => betIds.Contains(c.BetId)).Select(s => new BetSelectionMemory 
            {
                BetId = s.BetId,
                FixtureId = s.FixtureId,
                FixtureSpecifier = s.FixtureSpecifier,
                MarketTemplateId = s.MarketTemplateId,
                SelectionSpecifier = s.SelectionSpecifier,
                SelectionTemplateId = s.SelectionTemplateId,
                Probability = s.Probability
            }).ToList();

            foreach (var bet in bets)
            {
                var selections = betSelections.Where(c => c.BetId == bet.BetId).ToList();

                if (selections.Count() > 0)
                {
                    bet.BetSelections.AddRange(selections);

                    foreach (var selection in selections)
                    {
                        var copy = _fixtureBets.Where(c => c.Item1 == selection.FixtureId).FirstOrDefault();

                        if (copy != null)
                        {
                            if (copy.Item2.Any(c => c.BetId == selection.BetId)) continue;

                            copy.Item2.Add(bet);
                        }
                        else
                            _fixtureBets.Add(new Tuple<long, List<BetMemory>>(selection.FixtureId, new List<BetMemory> { bet }));
                    }
                }
            }
        }

        public void AddBet(OnBetPlacement message)
        {
            if (message.BetSelections.Any(c => c.SportId > 1000) || message.BetSelections.Any(c => c.SportId == 21) || message.BetSelections.Any(c => c.FixtureTypeId == (int)FixtureType.Outright))
                return;

            foreach (var selection in message.BetSelections)
            {
                var copy = _fixtureBets.Where(c => c.Item1 == selection.FixtureId).FirstOrDefault();

                var bet = new BetMemory
                {
                    BetId = message.Bet.BetId,
                    ClientId = message.Bet.ClientId,
                    UserId = message.Bet.UserId,
                    BetStatusId = message.Bet.BetStatusId,
                    TrueStake = message.Bet.TrueStake,
                    Odds = message.Bet.Odds,
                    BetSelections = message.BetSelections.Select(s => new BetSelectionMemory
                    {
                        BetId = s.BetId,
                        FixtureId = s.FixtureId,
                        FixtureSpecifier = s.FixtureSpecifier,
                        MarketTemplateId = s.MarketTemplateId,
                        SelectionSpecifier = s.SelectionSpecifier,
                        SelectionTemplateId = s.SelectionTemplateId,
                        Probability = s.Probability
                    }).ToList()
                };

                if (copy != null)
                {
                    if (copy.Item2.Any(c => c.BetId == selection.BetId)) continue;

                    copy.Item2.Add(bet);
                }
                else
                    _fixtureBets.Add(new Tuple<long, List<BetMemory>>(selection.FixtureId, new List<BetMemory> { bet }));
            }
        }

        public void RemoveBet(OnBetSettlement message)
        {
            if (message.BetSelections.Any(c => c.SportId > 1000) || message.BetSelections.Any(c => c.SportId == 21) || message.BetSelections.Any(c => c.FixtureTypeId == (int)FixtureType.Outright))
                return;

            foreach (var selection in message.BetSelections)
            {
                var copy = _fixtureBets.Where(c => c.Item1 == selection.FixtureId).FirstOrDefault();

                if (copy != null)
                {
                    var toRemove = copy.Item2.Where(c => c.BetId == message.Bet.BetId).FirstOrDefault();

                    if (toRemove != null)
                        copy.Item2.Remove(toRemove);
                }
            }
        }

        public void ProcessCashOutStake(OnInternalOddsChangeMessage message, IMessageBroker messageBroker)
        {
            if (message.SportId > 1000 || message.SportId == 21 || message.FixtureType == FixtureType.Outright || message.FixtureStatus == FixtureStatus.Prematch)
                return;

            var fixtureBet = _fixtureBets.Where(c => c.Item1 == message.FixtureId).FirstOrDefault();

            if (fixtureBet != null)
            {
                foreach (var bet in fixtureBet.Item2)
                {
                    Process(bet, bet.BetSelections, messageBroker);
                }
            }
        }

        public void Process(BetMemory bet, List<BetSelectionMemory> betSelections, IMessageBroker messageBroker)
        {
            if (bet.BetStatusId == (int)BetStatus.Running)
            {
                try
                {
                    var probabilites = new List<Probabilities>();
                    var hasProbabilityChanged = false;

                    foreach (var betSelection in betSelections)
                    {
                        HttpResponseMessage response;

                        if (betSelection.SelectionSpecifier.Contains("?"))
                            response = _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}/{betSelection.SelectionSpecifier.Split('?').Last().Replace('&', '|')}").Result;
                        else
                            response = _httpClient.GetAsync($"v1/probabilities/{betSelection.FixtureSpecifier}/{betSelection.MarketTemplateId}").Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var result = response.Content.ReadAsStringAsync().Result;

                            XmlDocument doc = new XmlDocument();

                            doc.LoadXml(result);

                            var node = doc.ChildNodes[1].ChildNodes[1].ChildNodes[0];

                            var cashOutStatusId = Convert.ToInt32(node.Attributes["cashout_status"].Value);

                            if (cashOutStatusId == 1)
                            {
                                foreach (XmlElement selection in node.ChildNodes)
                                {
                                    var sel = selection.Attributes["id"].Value;

                                    if (sel == betSelection.SelectionTemplateId)
                                    {
                                        double wonProbability = 0;
                                        double lostProbability = 0;
                                        double voidProbability = 0;
                                        double halfWonProbability = 0;
                                        double halfLostProbability = 0;

                                        try
                                        {
                                            wonProbability = Convert.ToDouble(selection.Attributes["win_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            lostProbability = Convert.ToDouble(selection.Attributes["lose_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            voidProbability = Convert.ToDouble(selection.Attributes["refund_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            halfWonProbability = Convert.ToDouble(selection.Attributes["half_win_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        try
                                        {
                                            halfLostProbability = Convert.ToDouble(selection.Attributes["half_lose_probabilities"].Value);
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        if (Math.Round(Convert.ToDecimal(betSelection.Probability), 2) != Math.Round(Convert.ToDecimal(selection.Attributes["probabilities"].Value), 2))
                                            hasProbabilityChanged = true;

                                        var probability = Convert.ToDouble(selection.Attributes["probabilities"].Value);

                                        probabilites.Add(new Probabilities
                                        {
                                            CashOutStatusId = 1,
                                            Probability = probability,
                                            WonProbability = wonProbability,
                                            LostProbability = lostProbability,
                                            VoidProbability = voidProbability,
                                            HalfWonProbability = halfWonProbability,
                                            HalfLostProbability = halfLostProbability
                                        });
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                probabilites.Add(new Probabilities
                                {
                                    CashOutStatusId = -1,
                                    Probability = 0
                                });
                            }
                        }
                        else
                        {
                            probabilites.Add(new Probabilities
                            {
                                CashOutStatusId = -1,
                                Probability = 0
                            });
                        }
                    }

                    if (betSelections.Count > 1)
                    {
                        if (probabilites.Any(c => c.CashOutStatusId != (int)CashOutStatus.Available))
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            }, messageBroker);

                            return;
                        }

                        if (!probabilites.Any(c => c.CashOutStatusId == (int)CashOutStatus.Available))
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            }, messageBroker);

                            return;
                        }

                        var finalProbability = probabilites.Where(c => c.Probability > 0).Select(s => s.Probability).Aggregate((a, b) => a * b);

                        var cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(finalProbability);

                        var finalCashOutStake = cashOutStake - (cashOutStake * 0.10m);

                        if (!hasProbabilityChanged)
                            finalCashOutStake = bet.TrueStake - (bet.TrueStake * 0.10m);

                        if (finalCashOutStake == 0 || finalCashOutStake > bet.TrueStake * bet.Odds)
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            }, messageBroker);

                            return;
                        }
                        else
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Available,
                                CashOutStake = finalCashOutStake
                            }, messageBroker);

                            return;
                        }
                    }
                    else
                    {
                        if (probabilites.First().CashOutStatusId != (int)CashOutStatus.Available)
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            }, messageBroker);

                            return;
                        }

                        decimal cashOutStake = 0m;

                        if (probabilites.First().WonProbability > 0 ||
                            probabilites.First().LostProbability > 0 ||
                            probabilites.First().VoidProbability > 0 ||
                            probabilites.First().HalfWonProbability > 0 ||
                            probabilites.First().HalfLostProbability > 0)
                        {
                            var won = Convert.ToDecimal(probabilites.First().WonProbability) * (bet.TrueStake * bet.Odds);
                            var lost = Convert.ToDecimal(probabilites.First().LostProbability) * (bet.TrueStake * 0);
                            var voidd = Convert.ToDecimal(probabilites.First().VoidProbability) * (bet.TrueStake * 1);
                            var halfWon = Convert.ToDecimal(probabilites.First().HalfWonProbability) * (bet.TrueStake * ((bet.Odds + 1) / 2));
                            var halfLost = Convert.ToDecimal(probabilites.First().HalfLostProbability) * (bet.TrueStake / 2);

                            cashOutStake = won + lost + voidd + halfWon + halfLost;
                        }
                        else
                            cashOutStake = (bet.TrueStake * bet.Odds) * Convert.ToDecimal(probabilites.First().Probability);

                        var finalCashOutStake = cashOutStake - (cashOutStake * 0.10m);

                        if (!hasProbabilityChanged)
                            finalCashOutStake = bet.TrueStake - (bet.TrueStake * 0.10m);

                        if (finalCashOutStake <= bet.TrueStake * bet.Odds)
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Available,
                                CashOutStake = finalCashOutStake
                            }, messageBroker);

                            return;
                        }
                        else
                        {
                            Publish(new OnCashOutChange
                            {
                                BetId = bet.BetId.ToString().ToUpper(),
                                ClientId = bet.ClientId,
                                UserId = bet.UserId,
                                CashOutStatus = CashOutStatus.Unavailable,
                                CashOutStake = 0m
                            }, messageBroker);

                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    Publish(new OnCashOutChange
                    {
                        BetId = bet.BetId.ToString().ToUpper(),
                        ClientId = bet.ClientId,
                        UserId = bet.UserId,
                        CashOutStatus = CashOutStatus.Unavailable,
                        CashOutStake = 0m
                    }, messageBroker);

                    return;
                }
            }
            else
            {
                Publish(new OnCashOutChange
                {
                    BetId = bet.BetId.ToString().ToUpper(),
                    ClientId = bet.ClientId,
                    UserId = bet.UserId,
                    CashOutStatus = CashOutStatus.Unavailable,
                    CashOutStake = 0m
                }, messageBroker);

                return;
            }
        }

        public void Publish(OnCashOutChange message, IMessageBroker messageBroker)
        {
            messageBroker.ExternalPublish(message, "OnCashOutChange", $"r.bet.oncashoutchange", Guid.NewGuid().ToString());
            Console.WriteLine($"{DateTime.UtcNow} : {message.BetId} : {message.CashOutStatus.ToString()} : {message.CashOutStake}");
        }
    }

    public class BetMemory
    {
        public BetMemory()
        {
            BetSelections = new List<BetSelectionMemory>();
        }

        public Guid BetId { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public int BetStatusId { get; set; }
        public decimal TrueStake { get; set; }
        public decimal Odds { get; set; }
        public List<BetSelectionMemory> BetSelections { get; set; }
    }

    public class BetSelectionMemory
    {
        public Guid BetId { get; set; }
        public long FixtureId { get; set; }
        public string FixtureSpecifier { get; set; }
        public string MarketTemplateId { get; set; }
        public string SelectionSpecifier { get; set; }
        public string SelectionTemplateId { get; set; }
        public double Probability { get; set; }
    }

    public class Probabilities
    {
        public double WonProbability { get; set; }
        public double LostProbability { get; set; }
        public double VoidProbability { get; set; }
        public double HalfWonProbability { get; set; }
        public double HalfLostProbability { get; set; }
        public double Probability { get; set; }
        public int CashOutStatusId { get; set; }
    }
}

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

        public List<Tuple<long, List<BetMemory>>> _00fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _01fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _02fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _03fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _04fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _05fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _06fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _07fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _08fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _09fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _10fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _11fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _12fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _13fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _14fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _15fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _16fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _17fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _18fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _19fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _20fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _21fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _22fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _23fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _24fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _25fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _26fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _27fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _28fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _29fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _30fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _31fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _32fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _33fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _34fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _35fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _36fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _37fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _38fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _39fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _40fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _41fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _42fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _43fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _44fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _45fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _46fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _47fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _48fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _49fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _50fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _51fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _52fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _53fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _54fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _55fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _56fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _57fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _58fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _59fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _60fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _61fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _62fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _63fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _64fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _65fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _66fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _67fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _68fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _69fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _70fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _71fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _72fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _73fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _74fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _75fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _76fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _77fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _78fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _79fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _80fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _81fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _82fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _83fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _84fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _85fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _86fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _87fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _88fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _89fixtureBets = new List<Tuple<long, List<BetMemory>>>();

        public List<Tuple<long, List<BetMemory>>> _90fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _91fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _92fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _93fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _94fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _95fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _96fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _97fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _98fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        public List<Tuple<long, List<BetMemory>>> _99fixtureBets = new List<Tuple<long, List<BetMemory>>>();
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private void Add(List<Tuple<long, List<BetMemory>>> item, long fixtureId, BetMemory bet)
        {
            var copy = item.Where(c => c.Item1 == fixtureId).FirstOrDefault();

            if (copy != null)
            {
                if (copy.Item2.Any(c => c.BetId == bet.BetId)) return;

                copy.Item2.Add(bet);
            }
            else
                item.Add(new Tuple<long, List<BetMemory>>(fixtureId, new List<BetMemory> { bet }));
        }

        private void Remove(List<Tuple<long, List<BetMemory>>> item, long fixtureId, BetMemory bet)
        {
            var copy = item.Where(c => c.Item1 == fixtureId).FirstOrDefault();

            if (copy != null)
            {
                var toRemove = copy.Item2.Where(c => c.BetId == bet.BetId).FirstOrDefault();

                if (toRemove != null)
                    copy.Item2.Remove(toRemove);
            }
        }

        private void ProcessCashOut(List<Tuple<long, List<BetMemory>>> item, long fixtureId, IMessageBroker messageBroker)
        {
            var fixtureBet = item.Where(c => c.Item1 == fixtureId).FirstOrDefault();

            if (fixtureBet != null)
            {
                foreach (var bet in fixtureBet.Item2)
                {
                    Process(bet, bet.BetSelections, messageBroker);
                }
            }
        }

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
                        var digit = Convert.ToString(selection.FixtureId);
                        var lastDigit = digit.Substring(digit.Length - 2);

                        switch (lastDigit)
                        {
                            case "00":
                                Add(_00fixtureBets, selection.FixtureId, bet);
                                break;
                            case "01":
                                Add(_01fixtureBets, selection.FixtureId, bet);
                                break;
                            case "02":
                                Add(_02fixtureBets, selection.FixtureId, bet);
                                break;
                            case "03":
                                Add(_03fixtureBets, selection.FixtureId, bet);
                                break;
                            case "04":
                                Add(_04fixtureBets, selection.FixtureId, bet);
                                break;
                            case "05":
                                Add(_05fixtureBets, selection.FixtureId, bet);
                                break;
                            case "06":
                                Add(_06fixtureBets, selection.FixtureId, bet);
                                break;
                            case "07":
                                Add(_07fixtureBets, selection.FixtureId, bet);
                                break;
                            case "08":
                                Add(_08fixtureBets, selection.FixtureId, bet);
                                break;
                            case "09":
                                Add(_09fixtureBets, selection.FixtureId, bet);
                                break;
                            case "10":
                                Add(_10fixtureBets, selection.FixtureId, bet);
                                break;
                            case "11":
                                Add(_11fixtureBets, selection.FixtureId, bet);
                                break;
                            case "12":
                                Add(_12fixtureBets, selection.FixtureId, bet);
                                break;
                            case "13":
                                Add(_13fixtureBets, selection.FixtureId, bet);
                                break;
                            case "14":
                                Add(_14fixtureBets, selection.FixtureId, bet);
                                break;
                            case "15":
                                Add(_15fixtureBets, selection.FixtureId, bet);
                                break;
                            case "16":
                                Add(_16fixtureBets, selection.FixtureId, bet);
                                break;
                            case "17":
                                Add(_17fixtureBets, selection.FixtureId, bet);
                                break;
                            case "18":
                                Add(_18fixtureBets, selection.FixtureId, bet);
                                break;
                            case "19":
                                Add(_19fixtureBets, selection.FixtureId, bet);
                                break;
                            case "20":
                                Add(_20fixtureBets, selection.FixtureId, bet);
                                break;
                            case "21":
                                Add(_21fixtureBets, selection.FixtureId, bet);
                                break;
                            case "22":
                                Add(_22fixtureBets, selection.FixtureId, bet);
                                break;
                            case "23":
                                Add(_23fixtureBets, selection.FixtureId, bet);
                                break;
                            case "24":
                                Add(_24fixtureBets, selection.FixtureId, bet);
                                break;
                            case "25":
                                Add(_25fixtureBets, selection.FixtureId, bet);
                                break;
                            case "26":
                                Add(_26fixtureBets, selection.FixtureId, bet);
                                break;
                            case "27":
                                Add(_27fixtureBets, selection.FixtureId, bet);
                                break;
                            case "28":
                                Add(_28fixtureBets, selection.FixtureId, bet);
                                break;
                            case "29":
                                Add(_29fixtureBets, selection.FixtureId, bet);
                                break;
                            case "30":
                                Add(_30fixtureBets, selection.FixtureId, bet);
                                break;
                            case "31":
                                Add(_31fixtureBets, selection.FixtureId, bet);
                                break;
                            case "32":
                                Add(_32fixtureBets, selection.FixtureId, bet);
                                break;
                            case "33":
                                Add(_33fixtureBets, selection.FixtureId, bet);
                                break;
                            case "34":
                                Add(_34fixtureBets, selection.FixtureId, bet);
                                break;
                            case "35":
                                Add(_35fixtureBets, selection.FixtureId, bet);
                                break;
                            case "36":
                                Add(_36fixtureBets, selection.FixtureId, bet);
                                break;
                            case "37":
                                Add(_37fixtureBets, selection.FixtureId, bet);
                                break;
                            case "38":
                                Add(_38fixtureBets, selection.FixtureId, bet);
                                break;
                            case "39":
                                Add(_39fixtureBets, selection.FixtureId, bet);
                                break;
                            case "40":
                                Add(_40fixtureBets, selection.FixtureId, bet);
                                break;
                            case "41":
                                Add(_41fixtureBets, selection.FixtureId, bet);
                                break;
                            case "42":
                                Add(_42fixtureBets, selection.FixtureId, bet);
                                break;
                            case "43":
                                Add(_43fixtureBets, selection.FixtureId, bet);
                                break;
                            case "44":
                                Add(_44fixtureBets, selection.FixtureId, bet);
                                break;
                            case "45":
                                Add(_45fixtureBets, selection.FixtureId, bet);
                                break;
                            case "46":
                                Add(_46fixtureBets, selection.FixtureId, bet);
                                break;
                            case "47":
                                Add(_47fixtureBets, selection.FixtureId, bet);
                                break;
                            case "48":
                                Add(_48fixtureBets, selection.FixtureId, bet);
                                break;
                            case "49":
                                Add(_49fixtureBets, selection.FixtureId, bet);
                                break;
                            case "50":
                                Add(_50fixtureBets, selection.FixtureId, bet);
                                break;
                            case "51":
                                Add(_51fixtureBets, selection.FixtureId, bet);
                                break;
                            case "52":
                                Add(_52fixtureBets, selection.FixtureId, bet);
                                break;
                            case "53":
                                Add(_53fixtureBets, selection.FixtureId, bet);
                                break;
                            case "54":
                                Add(_54fixtureBets, selection.FixtureId, bet);
                                break;
                            case "55":
                                Add(_55fixtureBets, selection.FixtureId, bet);
                                break;
                            case "56":
                                Add(_56fixtureBets, selection.FixtureId, bet);
                                break;
                            case "57":
                                Add(_57fixtureBets, selection.FixtureId, bet);
                                break;
                            case "58":
                                Add(_58fixtureBets, selection.FixtureId, bet);
                                break;
                            case "59":
                                Add(_59fixtureBets, selection.FixtureId, bet);
                                break;
                            case "60":
                                Add(_60fixtureBets, selection.FixtureId, bet);
                                break;
                            case "61":
                                Add(_61fixtureBets, selection.FixtureId, bet);
                                break;
                            case "62":
                                Add(_62fixtureBets, selection.FixtureId, bet);
                                break;
                            case "63":
                                Add(_63fixtureBets, selection.FixtureId, bet);
                                break;
                            case "64":
                                Add(_64fixtureBets, selection.FixtureId, bet);
                                break;
                            case "65":
                                Add(_65fixtureBets, selection.FixtureId, bet);
                                break;
                            case "66":
                                Add(_66fixtureBets, selection.FixtureId, bet);
                                break;
                            case "67":
                                Add(_67fixtureBets, selection.FixtureId, bet);
                                break;
                            case "68":
                                Add(_68fixtureBets, selection.FixtureId, bet);
                                break;
                            case "69":
                                Add(_69fixtureBets, selection.FixtureId, bet);
                                break;
                            case "70":
                                Add(_70fixtureBets, selection.FixtureId, bet);
                                break;
                            case "71":
                                Add(_71fixtureBets, selection.FixtureId, bet);
                                break;
                            case "72":
                                Add(_72fixtureBets, selection.FixtureId, bet);
                                break;
                            case "73":
                                Add(_74fixtureBets, selection.FixtureId, bet);
                                break;
                            case "74":
                                Add(_74fixtureBets, selection.FixtureId, bet);
                                break;
                            case "75":
                                Add(_75fixtureBets, selection.FixtureId, bet);
                                break;
                            case "76":
                                Add(_76fixtureBets, selection.FixtureId, bet);
                                break;
                            case "77":
                                Add(_77fixtureBets, selection.FixtureId, bet);
                                break;
                            case "78":
                                Add(_78fixtureBets, selection.FixtureId, bet);
                                break;
                            case "79":
                                Add(_79fixtureBets, selection.FixtureId, bet);
                                break;
                            case "80":
                                Add(_80fixtureBets, selection.FixtureId, bet);
                                break;
                            case "81":
                                Add(_81fixtureBets, selection.FixtureId, bet);
                                break;
                            case "82":
                                Add(_82fixtureBets, selection.FixtureId, bet);
                                break;
                            case "83":
                                Add(_83fixtureBets, selection.FixtureId, bet);
                                break;
                            case "84":
                                Add(_84fixtureBets, selection.FixtureId, bet);
                                break;
                            case "85":
                                Add(_85fixtureBets, selection.FixtureId, bet);
                                break;
                            case "86":
                                Add(_86fixtureBets, selection.FixtureId, bet);
                                break;
                            case "87":
                                Add(_87fixtureBets, selection.FixtureId, bet);
                                break;
                            case "88":
                                Add(_88fixtureBets, selection.FixtureId, bet);
                                break;
                            case "89":
                                Add(_89fixtureBets, selection.FixtureId, bet);
                                break;
                            case "90":
                                Add(_90fixtureBets, selection.FixtureId, bet);
                                break;
                            case "91":
                                Add(_91fixtureBets, selection.FixtureId, bet);
                                break;
                            case "92":
                                Add(_92fixtureBets, selection.FixtureId, bet);
                                break;
                            case "93":
                                Add(_93fixtureBets, selection.FixtureId, bet);
                                break;
                            case "94":
                                Add(_94fixtureBets, selection.FixtureId, bet);
                                break;
                            case "95":
                                Add(_95fixtureBets, selection.FixtureId, bet);
                                break;
                            case "96":
                                Add(_96fixtureBets, selection.FixtureId, bet);
                                break;
                            case "97":
                                Add(_97fixtureBets, selection.FixtureId, bet);
                                break;
                            case "98":
                                Add(_98fixtureBets, selection.FixtureId, bet);
                                break;
                            case "99":
                                Add(_99fixtureBets, selection.FixtureId, bet);
                                break;
                        }
                    }
                }
            }
        }

        public void AddBet(OnBetPlacement message, long fixtureId)
        {
            if (message.BetSelections.Any(c => c.SportId > 1000) || message.BetSelections.Any(c => c.SportId == 21) || message.BetSelections.Any(c => c.FixtureTypeId == (int)FixtureType.Outright))
                return;

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

            var digit = Convert.ToString(fixtureId);
            var lastDigit = digit.Substring(digit.Length - 2);

            switch (lastDigit)
            {
                case "00":
                    Add(_00fixtureBets, fixtureId, bet);
                    break;
                case "01":
                    Add(_01fixtureBets, fixtureId, bet);
                    break;
                case "02":
                    Add(_02fixtureBets, fixtureId, bet);
                    break;
                case "03":
                    Add(_03fixtureBets, fixtureId, bet);
                    break;
                case "04":
                    Add(_04fixtureBets, fixtureId, bet);
                    break;
                case "05":
                    Add(_05fixtureBets, fixtureId, bet);
                    break;
                case "06":
                    Add(_06fixtureBets, fixtureId, bet);
                    break;
                case "07":
                    Add(_07fixtureBets, fixtureId, bet);
                    break;
                case "08":
                    Add(_08fixtureBets, fixtureId, bet);
                    break;
                case "09":
                    Add(_09fixtureBets, fixtureId, bet);
                    break;
                case "10":
                    Add(_10fixtureBets, fixtureId, bet);
                    break;
                case "11":
                    Add(_11fixtureBets, fixtureId, bet);
                    break;
                case "12":
                    Add(_12fixtureBets, fixtureId, bet);
                    break;
                case "13":
                    Add(_13fixtureBets, fixtureId, bet);
                    break;
                case "14":
                    Add(_14fixtureBets, fixtureId, bet);
                    break;
                case "15":
                    Add(_15fixtureBets, fixtureId, bet);
                    break;
                case "16":
                    Add(_16fixtureBets, fixtureId, bet);
                    break;
                case "17":
                    Add(_17fixtureBets, fixtureId, bet);
                    break;
                case "18":
                    Add(_18fixtureBets, fixtureId, bet);
                    break;
                case "19":
                    Add(_19fixtureBets, fixtureId, bet);
                    break;
                case "20":
                    Add(_20fixtureBets, fixtureId, bet);
                    break;
                case "21":
                    Add(_21fixtureBets, fixtureId, bet);
                    break;
                case "22":
                    Add(_22fixtureBets, fixtureId, bet);
                    break;
                case "23":
                    Add(_23fixtureBets, fixtureId, bet);
                    break;
                case "24":
                    Add(_24fixtureBets, fixtureId, bet);
                    break;
                case "25":
                    Add(_25fixtureBets, fixtureId, bet);
                    break;
                case "26":
                    Add(_26fixtureBets, fixtureId, bet);
                    break;
                case "27":
                    Add(_27fixtureBets, fixtureId, bet);
                    break;
                case "28":
                    Add(_28fixtureBets, fixtureId, bet);
                    break;
                case "29":
                    Add(_29fixtureBets, fixtureId, bet);
                    break;
                case "30":
                    Add(_30fixtureBets, fixtureId, bet);
                    break;
                case "31":
                    Add(_31fixtureBets, fixtureId, bet);
                    break;
                case "32":
                    Add(_32fixtureBets, fixtureId, bet);
                    break;
                case "33":
                    Add(_33fixtureBets, fixtureId, bet);
                    break;
                case "34":
                    Add(_34fixtureBets, fixtureId, bet);
                    break;
                case "35":
                    Add(_35fixtureBets, fixtureId, bet);
                    break;
                case "36":
                    Add(_36fixtureBets, fixtureId, bet);
                    break;
                case "37":
                    Add(_37fixtureBets, fixtureId, bet);
                    break;
                case "38":
                    Add(_38fixtureBets, fixtureId, bet);
                    break;
                case "39":
                    Add(_39fixtureBets, fixtureId, bet);
                    break;
                case "40":
                    Add(_40fixtureBets, fixtureId, bet);
                    break;
                case "41":
                    Add(_41fixtureBets, fixtureId, bet);
                    break;
                case "42":
                    Add(_42fixtureBets, fixtureId, bet);
                    break;
                case "43":
                    Add(_43fixtureBets, fixtureId, bet);
                    break;
                case "44":
                    Add(_44fixtureBets, fixtureId, bet);
                    break;
                case "45":
                    Add(_45fixtureBets, fixtureId, bet);
                    break;
                case "46":
                    Add(_46fixtureBets, fixtureId, bet);
                    break;
                case "47":
                    Add(_47fixtureBets, fixtureId, bet);
                    break;
                case "48":
                    Add(_48fixtureBets, fixtureId, bet);
                    break;
                case "49":
                    Add(_49fixtureBets, fixtureId, bet);
                    break;
                case "50":
                    Add(_50fixtureBets, fixtureId, bet);
                    break;
                case "51":
                    Add(_51fixtureBets, fixtureId, bet);
                    break;
                case "52":
                    Add(_52fixtureBets, fixtureId, bet);
                    break;
                case "53":
                    Add(_53fixtureBets, fixtureId, bet);
                    break;
                case "54":
                    Add(_54fixtureBets, fixtureId, bet);
                    break;
                case "55":
                    Add(_55fixtureBets, fixtureId, bet);
                    break;
                case "56":
                    Add(_56fixtureBets, fixtureId, bet);
                    break;
                case "57":
                    Add(_57fixtureBets, fixtureId, bet);
                    break;
                case "58":
                    Add(_58fixtureBets, fixtureId, bet);
                    break;
                case "59":
                    Add(_59fixtureBets, fixtureId, bet);
                    break;
                case "60":
                    Add(_60fixtureBets, fixtureId, bet);
                    break;
                case "61":
                    Add(_61fixtureBets, fixtureId, bet);
                    break;
                case "62":
                    Add(_62fixtureBets, fixtureId, bet);
                    break;
                case "63":
                    Add(_63fixtureBets, fixtureId, bet);
                    break;
                case "64":
                    Add(_64fixtureBets, fixtureId, bet);
                    break;
                case "65":
                    Add(_65fixtureBets, fixtureId, bet);
                    break;
                case "66":
                    Add(_66fixtureBets, fixtureId, bet);
                    break;
                case "67":
                    Add(_67fixtureBets, fixtureId, bet);
                    break;
                case "68":
                    Add(_68fixtureBets, fixtureId, bet);
                    break;
                case "69":
                    Add(_69fixtureBets, fixtureId, bet);
                    break;
                case "70":
                    Add(_70fixtureBets, fixtureId, bet);
                    break;
                case "71":
                    Add(_71fixtureBets, fixtureId, bet);
                    break;
                case "72":
                    Add(_72fixtureBets, fixtureId, bet);
                    break;
                case "73":
                    Add(_74fixtureBets, fixtureId, bet);
                    break;
                case "74":
                    Add(_74fixtureBets, fixtureId, bet);
                    break;
                case "75":
                    Add(_75fixtureBets, fixtureId, bet);
                    break;
                case "76":
                    Add(_76fixtureBets, fixtureId, bet);
                    break;
                case "77":
                    Add(_77fixtureBets, fixtureId, bet);
                    break;
                case "78":
                    Add(_78fixtureBets, fixtureId, bet);
                    break;
                case "79":
                    Add(_79fixtureBets, fixtureId, bet);
                    break;
                case "80":
                    Add(_80fixtureBets, fixtureId, bet);
                    break;
                case "81":
                    Add(_81fixtureBets, fixtureId, bet);
                    break;
                case "82":
                    Add(_82fixtureBets, fixtureId, bet);
                    break;
                case "83":
                    Add(_83fixtureBets, fixtureId, bet);
                    break;
                case "84":
                    Add(_84fixtureBets, fixtureId, bet);
                    break;
                case "85":
                    Add(_85fixtureBets, fixtureId, bet);
                    break;
                case "86":
                    Add(_86fixtureBets, fixtureId, bet);
                    break;
                case "87":
                    Add(_87fixtureBets, fixtureId, bet);
                    break;
                case "88":
                    Add(_88fixtureBets, fixtureId, bet);
                    break;
                case "89":
                    Add(_89fixtureBets, fixtureId, bet);
                    break;
                case "90":
                    Add(_90fixtureBets, fixtureId, bet);
                    break;
                case "91":
                    Add(_91fixtureBets, fixtureId, bet);
                    break;
                case "92":
                    Add(_92fixtureBets, fixtureId, bet);
                    break;
                case "93":
                    Add(_93fixtureBets, fixtureId, bet);
                    break;
                case "94":
                    Add(_94fixtureBets, fixtureId, bet);
                    break;
                case "95":
                    Add(_95fixtureBets, fixtureId, bet);
                    break;
                case "96":
                    Add(_96fixtureBets, fixtureId, bet);
                    break;
                case "97":
                    Add(_97fixtureBets, fixtureId, bet);
                    break;
                case "98":
                    Add(_98fixtureBets, fixtureId, bet);
                    break;
                case "99":
                    Add(_99fixtureBets, fixtureId, bet);
                    break;
            }
        }

        public void RemoveBet(OnBetSettlement message, long fixtureId)
        {
            if (message.BetSelections.Any(c => c.SportId > 1000) || message.BetSelections.Any(c => c.SportId == 21) || message.BetSelections.Any(c => c.FixtureTypeId == (int)FixtureType.Outright))
                return;

            var digit = Convert.ToString(fixtureId);
            var lastDigit = digit.Substring(digit.Length - 2);

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

            switch (lastDigit)
            {
                case "00":
                    Remove(_00fixtureBets, fixtureId, bet);
                    break;
                case "01":
                    Remove(_01fixtureBets, fixtureId, bet);
                    break;
                case "02":
                    Remove(_02fixtureBets, fixtureId, bet);
                    break;
                case "03":
                    Remove(_03fixtureBets, fixtureId, bet);
                    break;
                case "04":
                    Remove(_04fixtureBets, fixtureId, bet);
                    break;
                case "05":
                    Remove(_05fixtureBets, fixtureId, bet);
                    break;
                case "06":
                    Remove(_06fixtureBets, fixtureId, bet);
                    break;
                case "07":
                    Remove(_07fixtureBets, fixtureId, bet);
                    break;
                case "08":
                    Remove(_08fixtureBets, fixtureId, bet);
                    break;
                case "09":
                    Remove(_09fixtureBets, fixtureId, bet);
                    break;
                case "10":
                    Remove(_10fixtureBets, fixtureId, bet);
                    break;
                case "11":
                    Remove(_11fixtureBets, fixtureId, bet);
                    break;
                case "12":
                    Remove(_12fixtureBets, fixtureId, bet);
                    break;
                case "13":
                    Remove(_13fixtureBets, fixtureId, bet);
                    break;
                case "14":
                    Remove(_14fixtureBets, fixtureId, bet);
                    break;
                case "15":
                    Remove(_15fixtureBets, fixtureId, bet);
                    break;
                case "16":
                    Remove(_16fixtureBets, fixtureId, bet);
                    break;
                case "17":
                    Remove(_17fixtureBets, fixtureId, bet);
                    break;
                case "18":
                    Remove(_18fixtureBets, fixtureId, bet);
                    break;
                case "19":
                    Remove(_19fixtureBets, fixtureId, bet);
                    break;
                case "20":
                    Remove(_20fixtureBets, fixtureId, bet);
                    break;
                case "21":
                    Remove(_21fixtureBets, fixtureId, bet);
                    break;
                case "22":
                    Remove(_22fixtureBets, fixtureId, bet);
                    break;
                case "23":
                    Remove(_23fixtureBets, fixtureId, bet);
                    break;
                case "24":
                    Remove(_24fixtureBets, fixtureId, bet);
                    break;
                case "25":
                    Remove(_25fixtureBets, fixtureId, bet);
                    break;
                case "26":
                    Remove(_26fixtureBets, fixtureId, bet);
                    break;
                case "27":
                    Remove(_27fixtureBets, fixtureId, bet);
                    break;
                case "28":
                    Remove(_28fixtureBets, fixtureId, bet);
                    break;
                case "29":
                    Remove(_29fixtureBets, fixtureId, bet);
                    break;
                case "30":
                    Remove(_30fixtureBets, fixtureId, bet);
                    break;
                case "31":
                    Remove(_31fixtureBets, fixtureId, bet);
                    break;
                case "32":
                    Remove(_32fixtureBets, fixtureId, bet);
                    break;
                case "33":
                    Remove(_33fixtureBets, fixtureId, bet);
                    break;
                case "34":
                    Remove(_34fixtureBets, fixtureId, bet);
                    break;
                case "35":
                    Remove(_35fixtureBets, fixtureId, bet);
                    break;
                case "36":
                    Remove(_36fixtureBets, fixtureId, bet);
                    break;
                case "37":
                    Remove(_37fixtureBets, fixtureId, bet);
                    break;
                case "38":
                    Remove(_38fixtureBets, fixtureId, bet);
                    break;
                case "39":
                    Remove(_39fixtureBets, fixtureId, bet);
                    break;
                case "40":
                    Remove(_40fixtureBets, fixtureId, bet);
                    break;
                case "41":
                    Remove(_41fixtureBets, fixtureId, bet);
                    break;
                case "42":
                    Remove(_42fixtureBets, fixtureId, bet);
                    break;
                case "43":
                    Remove(_43fixtureBets, fixtureId, bet);
                    break;
                case "44":
                    Remove(_44fixtureBets, fixtureId, bet);
                    break;
                case "45":
                    Remove(_45fixtureBets, fixtureId, bet);
                    break;
                case "46":
                    Remove(_46fixtureBets, fixtureId, bet);
                    break;
                case "47":
                    Remove(_47fixtureBets, fixtureId, bet);
                    break;
                case "48":
                    Remove(_48fixtureBets, fixtureId, bet);
                    break;
                case "49":
                    Remove(_49fixtureBets, fixtureId, bet);
                    break;
                case "50":
                    Remove(_50fixtureBets, fixtureId, bet);
                    break;
                case "51":
                    Remove(_51fixtureBets, fixtureId, bet);
                    break;
                case "52":
                    Remove(_52fixtureBets, fixtureId, bet);
                    break;
                case "53":
                    Remove(_53fixtureBets, fixtureId, bet);
                    break;
                case "54":
                    Remove(_54fixtureBets, fixtureId, bet);
                    break;
                case "55":
                    Remove(_55fixtureBets, fixtureId, bet);
                    break;
                case "56":
                    Remove(_56fixtureBets, fixtureId, bet);
                    break;
                case "57":
                    Remove(_57fixtureBets, fixtureId, bet);
                    break;
                case "58":
                    Remove(_58fixtureBets, fixtureId, bet);
                    break;
                case "59":
                    Remove(_59fixtureBets, fixtureId, bet);
                    break;
                case "60":
                    Remove(_60fixtureBets, fixtureId, bet);
                    break;
                case "61":
                    Remove(_61fixtureBets, fixtureId, bet);
                    break;
                case "62":
                    Remove(_62fixtureBets, fixtureId, bet);
                    break;
                case "63":
                    Remove(_63fixtureBets, fixtureId, bet);
                    break;
                case "64":
                    Remove(_64fixtureBets, fixtureId, bet);
                    break;
                case "65":
                    Remove(_65fixtureBets, fixtureId, bet);
                    break;
                case "66":
                    Remove(_66fixtureBets, fixtureId, bet);
                    break;
                case "67":
                    Remove(_67fixtureBets, fixtureId, bet);
                    break;
                case "68":
                    Remove(_68fixtureBets, fixtureId, bet);
                    break;
                case "69":
                    Remove(_69fixtureBets, fixtureId, bet);
                    break;
                case "70":
                    Remove(_70fixtureBets, fixtureId, bet);
                    break;
                case "71":
                    Remove(_71fixtureBets, fixtureId, bet);
                    break;
                case "72":
                    Remove(_72fixtureBets, fixtureId, bet);
                    break;
                case "73":
                    Remove(_74fixtureBets, fixtureId, bet);
                    break;
                case "74":
                    Remove(_74fixtureBets, fixtureId, bet);
                    break;
                case "75":
                    Remove(_75fixtureBets, fixtureId, bet);
                    break;
                case "76":
                    Remove(_76fixtureBets, fixtureId, bet);
                    break;
                case "77":
                    Remove(_77fixtureBets, fixtureId, bet);
                    break;
                case "78":
                    Remove(_78fixtureBets, fixtureId, bet);
                    break;
                case "79":
                    Remove(_79fixtureBets, fixtureId, bet);
                    break;
                case "80":
                    Remove(_80fixtureBets, fixtureId, bet);
                    break;
                case "81":
                    Remove(_81fixtureBets, fixtureId, bet);
                    break;
                case "82":
                    Remove(_82fixtureBets, fixtureId, bet);
                    break;
                case "83":
                    Remove(_83fixtureBets, fixtureId, bet);
                    break;
                case "84":
                    Remove(_84fixtureBets, fixtureId, bet);
                    break;
                case "85":
                    Remove(_85fixtureBets, fixtureId, bet);
                    break;
                case "86":
                    Remove(_86fixtureBets, fixtureId, bet);
                    break;
                case "87":
                    Remove(_87fixtureBets, fixtureId, bet);
                    break;
                case "88":
                    Remove(_88fixtureBets, fixtureId, bet);
                    break;
                case "89":
                    Remove(_89fixtureBets, fixtureId, bet);
                    break;
                case "90":
                    Remove(_90fixtureBets, fixtureId, bet);
                    break;
                case "91":
                    Remove(_91fixtureBets, fixtureId, bet);
                    break;
                case "92":
                    Remove(_92fixtureBets, fixtureId, bet);
                    break;
                case "93":
                    Remove(_93fixtureBets, fixtureId, bet);
                    break;
                case "94":
                    Remove(_94fixtureBets, fixtureId, bet);
                    break;
                case "95":
                    Remove(_95fixtureBets, fixtureId, bet);
                    break;
                case "96":
                    Remove(_96fixtureBets, fixtureId, bet);
                    break;
                case "97":
                    Remove(_97fixtureBets, fixtureId, bet);
                    break;
                case "98":
                    Remove(_98fixtureBets, fixtureId, bet);
                    break;
                case "99":
                    Remove(_99fixtureBets, fixtureId, bet);
                    break;
            }
        }

        public void ProcessCashOutStake(OnInternalOddsChangeMessage message, long fixtureId, IMessageBroker messageBroker)
        {
            if (message.SportId > 1000 || message.SportId == 21 || message.FixtureType == FixtureType.Outright || message.FixtureStatus == FixtureStatus.Prematch)
                return;

            var digit = Convert.ToString(fixtureId);
            var lastDigit = digit.Substring(digit.Length - 2);

            switch (lastDigit)
            {
                case "00":
                    ProcessCashOut(_00fixtureBets, fixtureId, messageBroker);
                    break;
                case "01":
                    ProcessCashOut(_01fixtureBets, fixtureId, messageBroker);
                    break;
                case "02":
                    ProcessCashOut(_02fixtureBets, fixtureId, messageBroker);
                    break;
                case "03":
                    ProcessCashOut(_03fixtureBets, fixtureId, messageBroker);
                    break;
                case "04":
                    ProcessCashOut(_04fixtureBets, fixtureId, messageBroker);
                    break;
                case "05":
                    ProcessCashOut(_05fixtureBets, fixtureId, messageBroker);
                    break;
                case "06":
                    ProcessCashOut(_06fixtureBets, fixtureId, messageBroker);
                    break;
                case "07":
                    ProcessCashOut(_07fixtureBets, fixtureId, messageBroker);
                    break;
                case "08":
                    ProcessCashOut(_08fixtureBets, fixtureId, messageBroker);
                    break;
                case "09":
                    ProcessCashOut(_09fixtureBets, fixtureId, messageBroker);
                    break;
                case "10":
                    ProcessCashOut(_10fixtureBets, fixtureId, messageBroker);
                    break;
                case "11":
                    ProcessCashOut(_11fixtureBets, fixtureId, messageBroker);
                    break;
                case "12":
                    ProcessCashOut(_12fixtureBets, fixtureId, messageBroker);
                    break;
                case "13":
                    ProcessCashOut(_13fixtureBets, fixtureId, messageBroker);
                    break;
                case "14":
                    ProcessCashOut(_14fixtureBets, fixtureId, messageBroker);
                    break;
                case "15":
                    ProcessCashOut(_15fixtureBets, fixtureId, messageBroker);
                    break;
                case "16":
                    ProcessCashOut(_16fixtureBets, fixtureId, messageBroker);
                    break;
                case "17":
                    ProcessCashOut(_17fixtureBets, fixtureId, messageBroker);
                    break;
                case "18":
                    ProcessCashOut(_18fixtureBets, fixtureId, messageBroker);
                    break;
                case "19":
                    ProcessCashOut(_19fixtureBets, fixtureId, messageBroker);
                    break;
                case "20":
                    ProcessCashOut(_20fixtureBets, fixtureId, messageBroker);
                    break;
                case "21":
                    ProcessCashOut(_21fixtureBets, fixtureId, messageBroker);
                    break;
                case "22":
                    ProcessCashOut(_22fixtureBets, fixtureId, messageBroker);
                    break;
                case "23":
                    ProcessCashOut(_23fixtureBets, fixtureId, messageBroker);
                    break;
                case "24":
                    ProcessCashOut(_24fixtureBets, fixtureId, messageBroker);
                    break;
                case "25":
                    ProcessCashOut(_25fixtureBets, fixtureId, messageBroker);
                    break;
                case "26":
                    ProcessCashOut(_26fixtureBets, fixtureId, messageBroker);
                    break;
                case "27":
                    ProcessCashOut(_27fixtureBets, fixtureId, messageBroker);
                    break;
                case "28":
                    ProcessCashOut(_28fixtureBets, fixtureId, messageBroker);
                    break;
                case "29":
                    ProcessCashOut(_29fixtureBets, fixtureId, messageBroker);
                    break;
                case "30":
                    ProcessCashOut(_30fixtureBets, fixtureId, messageBroker);
                    break;
                case "31":
                    ProcessCashOut(_31fixtureBets, fixtureId, messageBroker);
                    break;
                case "32":
                    ProcessCashOut(_32fixtureBets, fixtureId, messageBroker);
                    break;
                case "33":
                    ProcessCashOut(_33fixtureBets, fixtureId, messageBroker);
                    break;
                case "34":
                    ProcessCashOut(_34fixtureBets, fixtureId, messageBroker);
                    break;
                case "35":
                    ProcessCashOut(_35fixtureBets, fixtureId, messageBroker);
                    break;
                case "36":
                    ProcessCashOut(_36fixtureBets, fixtureId, messageBroker);
                    break;
                case "37":
                    ProcessCashOut(_37fixtureBets, fixtureId, messageBroker);
                    break;
                case "38":
                    ProcessCashOut(_38fixtureBets, fixtureId, messageBroker);
                    break;
                case "39":
                    ProcessCashOut(_39fixtureBets, fixtureId, messageBroker);
                    break;
                case "40":
                    ProcessCashOut(_40fixtureBets, fixtureId, messageBroker);
                    break;
                case "41":
                    ProcessCashOut(_41fixtureBets, fixtureId, messageBroker);
                    break;
                case "42":
                    ProcessCashOut(_42fixtureBets, fixtureId, messageBroker);
                    break;
                case "43":
                    ProcessCashOut(_43fixtureBets, fixtureId, messageBroker);
                    break;
                case "44":
                    ProcessCashOut(_44fixtureBets, fixtureId, messageBroker);
                    break;
                case "45":
                    ProcessCashOut(_45fixtureBets, fixtureId, messageBroker);
                    break;
                case "46":
                    ProcessCashOut(_46fixtureBets, fixtureId, messageBroker);
                    break;
                case "47":
                    ProcessCashOut(_47fixtureBets, fixtureId, messageBroker);
                    break;
                case "48":
                    ProcessCashOut(_48fixtureBets, fixtureId, messageBroker);
                    break;
                case "49":
                    ProcessCashOut(_49fixtureBets, fixtureId, messageBroker);
                    break;
                case "50":
                    ProcessCashOut(_50fixtureBets, fixtureId, messageBroker);
                    break;
                case "51":
                    ProcessCashOut(_51fixtureBets, fixtureId, messageBroker);
                    break;
                case "52":
                    ProcessCashOut(_52fixtureBets, fixtureId, messageBroker);
                    break;
                case "53":
                    ProcessCashOut(_53fixtureBets, fixtureId, messageBroker);
                    break;
                case "54":
                    ProcessCashOut(_54fixtureBets, fixtureId, messageBroker);
                    break;
                case "55":
                    ProcessCashOut(_55fixtureBets, fixtureId, messageBroker);
                    break;
                case "56":
                    ProcessCashOut(_56fixtureBets, fixtureId, messageBroker);
                    break;
                case "57":
                    ProcessCashOut(_57fixtureBets, fixtureId, messageBroker);
                    break;
                case "58":
                    ProcessCashOut(_58fixtureBets, fixtureId, messageBroker);
                    break;
                case "59":
                    ProcessCashOut(_59fixtureBets, fixtureId, messageBroker);
                    break;
                case "60":
                    ProcessCashOut(_60fixtureBets, fixtureId, messageBroker);
                    break;
                case "61":
                    ProcessCashOut(_61fixtureBets, fixtureId, messageBroker);
                    break;
                case "62":
                    ProcessCashOut(_62fixtureBets, fixtureId, messageBroker);
                    break;
                case "63":
                    ProcessCashOut(_63fixtureBets, fixtureId, messageBroker);
                    break;
                case "64":
                    ProcessCashOut(_64fixtureBets, fixtureId, messageBroker);
                    break;
                case "65":
                    ProcessCashOut(_65fixtureBets, fixtureId, messageBroker);
                    break;
                case "66":
                    ProcessCashOut(_66fixtureBets, fixtureId, messageBroker);
                    break;
                case "67":
                    ProcessCashOut(_67fixtureBets, fixtureId, messageBroker);
                    break;
                case "68":
                    ProcessCashOut(_68fixtureBets, fixtureId, messageBroker);
                    break;
                case "69":
                    ProcessCashOut(_69fixtureBets, fixtureId, messageBroker);
                    break;
                case "70":
                    ProcessCashOut(_70fixtureBets, fixtureId, messageBroker);
                    break;
                case "71":
                    ProcessCashOut(_71fixtureBets, fixtureId, messageBroker);
                    break;
                case "72":
                    ProcessCashOut(_72fixtureBets, fixtureId, messageBroker);
                    break;
                case "73":
                    ProcessCashOut(_74fixtureBets, fixtureId, messageBroker);
                    break;
                case "74":
                    ProcessCashOut(_74fixtureBets, fixtureId, messageBroker);
                    break;
                case "75":
                    ProcessCashOut(_75fixtureBets, fixtureId, messageBroker);
                    break;
                case "76":
                    ProcessCashOut(_76fixtureBets, fixtureId, messageBroker);
                    break;
                case "77":
                    ProcessCashOut(_77fixtureBets, fixtureId, messageBroker);
                    break;
                case "78":
                    ProcessCashOut(_78fixtureBets, fixtureId, messageBroker);
                    break;
                case "79":
                    ProcessCashOut(_79fixtureBets, fixtureId, messageBroker);
                    break;
                case "80":
                    ProcessCashOut(_80fixtureBets, fixtureId, messageBroker);
                    break;
                case "81":
                    ProcessCashOut(_81fixtureBets, fixtureId, messageBroker);
                    break;
                case "82":
                    ProcessCashOut(_82fixtureBets, fixtureId, messageBroker);
                    break;
                case "83":
                    ProcessCashOut(_83fixtureBets, fixtureId, messageBroker);
                    break;
                case "84":
                    ProcessCashOut(_84fixtureBets, fixtureId, messageBroker);
                    break;
                case "85":
                    ProcessCashOut(_85fixtureBets, fixtureId, messageBroker);
                    break;
                case "86":
                    ProcessCashOut(_86fixtureBets, fixtureId, messageBroker);
                    break;
                case "87":
                    ProcessCashOut(_87fixtureBets, fixtureId, messageBroker);
                    break;
                case "88":
                    ProcessCashOut(_88fixtureBets, fixtureId, messageBroker);
                    break;
                case "89":
                    ProcessCashOut(_89fixtureBets, fixtureId, messageBroker);
                    break;
                case "90":
                    ProcessCashOut(_90fixtureBets, fixtureId, messageBroker);
                    break;
                case "91":
                    ProcessCashOut(_91fixtureBets, fixtureId, messageBroker);
                    break;
                case "92":
                    ProcessCashOut(_92fixtureBets, fixtureId, messageBroker);
                    break;
                case "93":
                    ProcessCashOut(_93fixtureBets, fixtureId, messageBroker);
                    break;
                case "94":
                    ProcessCashOut(_94fixtureBets, fixtureId, messageBroker);
                    break;
                case "95":
                    ProcessCashOut(_95fixtureBets, fixtureId, messageBroker);
                    break;
                case "96":
                    ProcessCashOut(_96fixtureBets, fixtureId, messageBroker);
                    break;
                case "97":
                    ProcessCashOut(_97fixtureBets, fixtureId, messageBroker);
                    break;
                case "98":
                    ProcessCashOut(_98fixtureBets, fixtureId, messageBroker);
                    break;
                case "99":
                    ProcessCashOut(_99fixtureBets, fixtureId, messageBroker);
                    break;
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

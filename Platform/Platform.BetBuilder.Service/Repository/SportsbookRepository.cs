using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Data.Sportsbook.SQLEntities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;

namespace Platform.BetBuilder.Service.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private static IMongoDatabase _db;
        private readonly IConfiguration _configuration;
        private static IMongoCollection<Fixture> fixtureCollection;
        private static IMongoCollection<Market> marketCollection;
        private static IMongoCollection<MarketMapping> marketMappingCollection;
        private static IMongoCollection<Selection> selectionCollection;
        private static IMongoCollection<BettingStatus> bettingStatusCollection;

        public SportsbookRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            if (_db == null)
            {
                var client = new MongoClient(_configuration["ConnectionStrings:MongoDB-Sportsbook"]);
                _db = client.GetDatabase("Sportsbook");

                fixtureCollection = _db.GetCollection<Fixture>("Fixture").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                marketCollection = _db.GetCollection<Market>("Market").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                marketMappingCollection = _db.GetCollection<MarketMapping>("MarketMapping").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                selectionCollection = _db.GetCollection<Selection>("Selection").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));
                bettingStatusCollection = _db.GetCollection<BettingStatus>("BettingStatus").WithReadPreference(new ReadPreference(ReadPreferenceMode.Secondary));

            }

        }

        public async Task<BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>> GetBetBuilderSelectionsOdds(int sportId)
        {
            var SportId = sportId;
            Console.WriteLine($"{DateTime.UtcNow} - Starting to fetch SportId = {sportId} matches");

            FilterDefinition<Fixture> fixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == SportId);
            var fixtures = await fixtureCollection.Find(fixtureFilter).ToListAsync();



            var betBuilderMarketTemplates = GetBetBuilderMarketTemplates();
            var betBuilderSportIds = betBuilderMarketTemplates.Select(s => s.SportId).Distinct().ToList();

            var betBuilderMarketTemplateIds = betBuilderMarketTemplates.Where(b => b.SportId == SportId).Select(m => m.MarketTemplateId).Distinct().ToList();

            var fixtureIds = fixtures.Select(s => Convert.ToInt64(s.FiId)).Distinct().ToList();
            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && betBuilderMarketTemplateIds.Contains(c.MaTeId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr);
            var markets = await marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var marketIds = markets.Select(s => Convert.ToInt64(s.MaId)).Distinct().ToList();


            var marketMappingFilter = Builders<MarketMapping>.Filter.Where(c => marketIds.Contains(c.MaId));
            var marketMappingProjection = Builders<MarketMapping>.Projection.Include(c => c.PrFiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaSp);
            var marketMappings = marketMappingCollection.Find(marketMappingFilter).Project<MarketMappingFilter>(marketMappingProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketIds.Contains(c.MaId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr);
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var bettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(marketMappings, selections, bettingStatus);

            Console.WriteLine($"{DateTime.UtcNow} - Done fetch Soccer matches");

            if (selections.Result.Count == 0)
            {
                return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetBuilderMarketSelectionResponseDTO()
                };
            }
            
            var selectionOddsResponse = new BetBuilderMarketSelectionResponseDTO();

            var newFixtureIds = markets.Select(m => m.FiId).Distinct().ToList();
            var newFixtures = fixtures.Where(f => newFixtureIds.Contains(f.FiId)).ToList();

            foreach (var fixture in newFixtures)
            {
                foreach(var market in markets.Where(m => m.FiId == fixture.FiId))
                {

                    var isBetBuilder = false;
                    var betBuilderMarketTemplate = betBuilderMarketTemplates.FirstOrDefault(c => c.SportId == sportId && c.MarketTemplateId == market.MaTeId);

                    if (betBuilderMarketTemplate != null)
                    {
                        if (fixture.FiStId == (int)FixtureStatus.Prematch && betBuilderMarketTemplate.Prematch == true)
                        {
                            isBetBuilder = true;
                        }
                        else if (fixture.FiStId == (int)FixtureStatus.Live && betBuilderMarketTemplate.Live == true)
                        {
                            isBetBuilder = true;
                        }
                    }

                    if (isBetBuilder == false)
                        continue;

                    foreach (var selection in selections.Result.Where(s => s.MaId == market.MaId))
                    {
                        var marketResponse = await SRCalculateOddsFiltered(selection, fixture, marketMappings.Result.FirstOrDefault(m => m.MaId == market.MaId), marketMappings.Result, markets, selections.Result);

                        //log here
                        if(marketResponse != null && marketResponse.Markets.Any()) 
                            foreach(var response in marketResponse.Markets)
                            {
                                foreach(var sresponse in response.Selections)
                                {
                                    Console.WriteLine($"{selection.SeId}|{selection.SeNa}|{selection.SeTeId}|{response.MarketId}|{response.MarketName}|{sresponse.SelectionId}|{sresponse.SelectionName}|{sresponse.SelectionTemplateId}");
                                }
                            }
                    }

                }
            }

            return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new BetBuilderMarketSelectionResponseDTO()
            };
        }
        private List<BetBuilderMarketTemplates> GetBetBuilderMarketTemplates()
        {
            var result = new List<BetBuilderMarketTemplates>();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new System.Transactions.TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var optionsBuilder = new DbContextOptionsBuilder<SportsbookContext>().UseSqlServer(_configuration["ConnectionStrings:Sportsbook"]);

                using (var context = new SportsbookContext(optionsBuilder.Options))
                {
                    try
                    {
                        var betBuilder = context.BetBuilderMarketTemplates.AsNoTracking().ToList();
                        if (betBuilder.Count > 0)
                            result.AddRange(betBuilder);

                        scope.Complete();
                    }
                    catch (Exception)
                    {
                        return result;
                    }
                }
            }

            return result;

        }

        private async Task<BetBuilderMarketSelectionResponseDTO> SRCalculateOddsFiltered(SelectionFilter selection, Fixture fixture, MarketMappingFilter marketMapping, List<MarketMappingFilter> marketMappings, List<MarketFilter> markets, List<SelectionFilter> selections)
        {
            var selectionOddsResponse = new BetBuilderMarketSelectionResponseDTO();

            #region SR API to calculate odds
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.betradar.com");
                client.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

                var payload = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
                if (selection != null)
                {
                    payload = payload + $"<selections xmlns=\"http://schemas.sportradar.com/custombet/v1/endpoints\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://schemas.sportradar.com/custombet/v1/endpoints http://schemas.sportradar.com/custombet/v1/endpoints/Selections.xsd\">";

                    var marketSpecifier = marketMapping.MaSp;

                    if (string.IsNullOrEmpty(marketSpecifier))
                        payload = payload + $"<selection id=\"{fixture.FiSp}\" market_id=\"{marketMapping.MaTeId}\" outcome_id=\"{selection.SeTeId}\"/>";
                    else
                        payload = payload + $"<selection id=\"{fixture.FiSp}\" market_id=\"{marketMapping.MaTeId}\" specifiers=\"{marketSpecifier}\" outcome_id=\"{selection.SeTeId}\"/>";

                    payload = payload + $"</selections>";
                }

                var response = await client.PostAsync($"/v1/custombet/calculate-filter", new StringContent(payload, System.Text.Encoding.UTF8, "application/xml"));

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    XmlDocument doc = new XmlDocument();

                    doc.LoadXml(result);

                    #region Validate response

                    XmlElement root = doc.DocumentElement;

                    if (root.Name != "calculation_response")
                    {
                        return selectionOddsResponse;
                    }


                    foreach (XmlElement node in root.ChildNodes)
                    {
                        if (node.Name == "calculation")
                        {
                            selectionOddsResponse.TotalOdds = Convert.ToDecimal(node.Attributes["odds"].Value);
                            //selectionOddsResponse.Probability = Convert.ToDecimal(node.Attributes["probability"].Value);
                        }
                        else if (node.Name == "available_selections")
                        {
                            var eventNode = node.ChildNodes[0];

                            if (eventNode.ChildNodes.Count == 0)
                                continue;

                            var marketNodes = eventNode.ChildNodes[0];

                            var marketResponse = GetBetBuilderMarketsXmlFiltered(marketNodes.ChildNodes, marketMappings, markets, selections);
                            if (marketResponse.Any())
                                selectionOddsResponse.Markets.AddRange(marketResponse);
                        }
                    }

                    #endregion
                }
                return selectionOddsResponse;
            }

            #endregion
        }

        private List<BetBuilderMarketDTO> GetBetBuilderMarketsXmlFiltered(XmlNodeList list, List<MarketMappingFilter> marketMappings, List<MarketFilter> markets, List<SelectionFilter> selections)
        {
            var response = new List<BetBuilderMarketDTO>();

            foreach (XmlElement marketNode in list)
            {
                var marketTemplateId = marketNode.Attributes["id"].Value;
                var specifiers = marketNode.HasAttribute("specifiers") ? marketNode.Attributes["specifiers"].Value : string.Empty;
                var conflict = marketNode.HasAttribute("conflict") ? Convert.ToBoolean(marketNode.Attributes["conflict"].Value) : false;
                if (conflict == false)
                    continue;

                var marketMapped = marketMappings.FirstOrDefault(m => m.MaTeId == marketTemplateId && m.MaSp == specifiers);
                if (marketMapped == null)
                    continue;
                var market = markets.FirstOrDefault(m => marketMapped.MaId == m.MaId);
                if (market == null)
                    continue;

                var selectionResponse = new List<BetBuilderSelectionDTO>();

                foreach (XmlElement selectionNode in marketNode.ChildNodes)
                {
                    var selectionTemplateId = selectionNode.Attributes["id"].Value;
                    var sconflict = selectionNode.HasAttribute("conflict") ? Convert.ToBoolean(selectionNode.Attributes["conflict"].Value) : false;
                    if (sconflict == false)
                        continue;

                    var selection = selections.FirstOrDefault(m => marketMapped.MaId == m.MaId);
                    if (selection == null)
                        continue;

                    selectionResponse.Add(new BetBuilderSelectionDTO
                    {
                        SelectionId = selection.SeId.ToString(),
                        SelectionName = selection.SeNa,
                        SelectionTemplateId = selectionTemplateId,
                        IsActive = selection.IsAc,
                        CorrelationId = selection.CoId,
                        SortOrder = selection.SeSoOr
                    });
                }

                if (!selectionResponse.Any())
                    continue;

                response.Add(new BetBuilderMarketDTO
                {
                    MarketTemplateId = marketTemplateId,
                    MarketId = market.MaId.ToString(),
                    MarketName = market.MaNa,
                    Line = market.Li,
                    IsCashout = market.CaStId == 1 ? true : false,
                    MarketStatusId = market.MaStId,
                    MarketStatusName = market.MaStNa,
                    Selections = selectionResponse
                });

            }

            return response;
        }


    }
}

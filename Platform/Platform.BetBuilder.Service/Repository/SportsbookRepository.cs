using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Platform.Shared.Data.Sportsbook;
using Platform.Shared.Data.Sportsbook.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Platform.BetBuilder.Service.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private static IMongoDatabase _db;
        private readonly SportsbookContext _sportsbookContext;
        private readonly IConfiguration _configuration;
        private static IMongoCollection<Fixture> fixtureCollection;
        private static IMongoCollection<Market> marketCollection;
        private static IMongoCollection<MarketMapping> marketMappingCollection;
        private static IMongoCollection<Selection> selectionCollection;
        private static IMongoCollection<BettingStatus> bettingStatusCollection;
        private readonly HttpClient _httpClient;

        public SportsbookRepository(SportsbookContext sportsbookContext, HttpClient httpClient, IConfiguration configuration)
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

            _sportsbookContext = sportsbookContext;
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri("https://av-api.betradar.com");
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer D6ewpdvADa8k5U7Xcx");
        }

        public async Task<BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>> GetBetBuilderSelectionsOdds()
        {
            Console.WriteLine($"{DateTime.UtcNow} - Starting to fetch Soccer matches");

            FilterDefinition<Fixture> soccerfixtureFilter = Builders<Fixture>.Filter.Where(c => c.FiStId == (int)FixtureStatus.Prematch && c.FiTyId == (int)FixtureType.Match && c.SpId == 1 );
            var soccerfixtures = await fixtureCollection.Find(soccerfixtureFilter).ToListAsync();
            


            var fixtureIds = soccerfixtures.Select(s => Convert.ToInt64(s.FiId)).Distinct().ToList();
            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => fixtureIds.Contains(c.FiId) && marketStatusIds.Contains(c.MaStId) && marketTemplateIds.Contains(c.MaTeId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var marketIds = request.Selections.Select(s => Convert.ToInt64(s.MarketId)).Distinct().ToList();
            var selectionIds = request.Selections.Select(s => Convert.ToInt64(s.SelectionId)).Distinct().ToList();


            var marketStatusIds = new List<int> { (int)MarketStatus.Active, (int)MarketStatus.Suspended };

            var marketFilter = Builders<Market>.Filter.Where(c => marketIds.Contains(c.MaId) && marketStatusIds.Contains(c.MaStId));
            var marketProjection = Builders<Market>.Projection.Include(c => c.FiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaNa).Include(c => c.Li).Include(c => c.MaStId).Include(c => c.MaStNa).Include(c => c.MaStNa).Include(c => c.CaStId).Include(c => c.ClCo).Include(c => c.ClCoMa).Include(c => c.MaNaTr).ElemMatch(c => c.MaNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var markets = marketCollection.Find(marketFilter).Project<MarketFilter>(marketProjection).ToListAsync();

            var marketMappingFilter = Builders<MarketMapping>.Filter.Where(c => marketIds.Contains(c.MaId));
            var marketMappingProjection = Builders<MarketMapping>.Projection.Include(c => c.PrFiId).Include(c => c.MaTeId).Include(c => c.MaId).Include(c => c.MaSp);
            var marketMappings = marketMappingCollection.Find(marketMappingFilter).Project<MarketMappingFilter>(marketMappingProjection).ToListAsync();

            var selectionFilter = Builders<Selection>.Filter.Where(c => selectionIds.Contains(c.SeId) && marketStatusIds.Contains(c.MaStId));
            var selectionProjection = Builders<Selection>.Projection.Include(c => c.SeId).Include(c => c.FiId).Include(c => c.MaId).Include(c => c.SeTeId).Include(c => c.SeNa).Include(c => c.Od).Include(c => c.IsAc).Include(c => c.SeSoOr).Include(c => c.CoId).Include(c => c.SeNaTr).ElemMatch(c => c.SeNaTr, cc => cc.LaCo == request.LanguageCode.ToString());
            var selections = selectionCollection.Find(selectionFilter).Project<SelectionFilter>(selectionProjection).ToListAsync();

            var bettingStatusFilter = Builders<BettingStatus>.Filter.Where(c => c.BeSo == "betstop");
            var bettingStatus = bettingStatusCollection.Find(bettingStatusFilter).FirstOrDefaultAsync();

            await Task.WhenAll(fixtures, markets, marketMappings, selections, bettingStatus);

            var newFixtures = fixtures.Result;
            var newMarkets = markets.Result;
            var newMarketMappings = marketMappings.Result;
            var newSelections = selections.Result;
            var newBettingStatus = bettingStatus.Result;

            if (newSelections.Count == 0)
            {
                return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString(),
                    Result = new BetBuilderMarketSelectionResponseDTO()
                };
            }

            var selectionOddsResponse = new BetBuilderMarketSelectionResponseDTO();

            #region SR API to calculate odds
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.betradar.com");
                client.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");

                var payload = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
                if (request.Selections.Any())
                {
                    payload = payload + $"<selections xmlns=\"http://schemas.sportradar.com/custombet/v1/endpoints\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://schemas.sportradar.com/custombet/v1/endpoints http://schemas.sportradar.com/custombet/v1/endpoints/Selections.xsd\">";

                    foreach (var sel in request.Selections)
                    {
                        var fixture = fixtures.Result.FirstOrDefault(f => f.FiId == Convert.ToInt64(sel.FixtureId));
                        var marketMapping = marketMappings.Result.FirstOrDefault(f => f.MaId == Convert.ToInt64(sel.MarketId));
                        var selection = selections.Result.FirstOrDefault(s => s.SeId == Convert.ToInt64(sel.SelectionId));

                        var marketSpecifier = marketMapping.MaSp;

                        if (string.IsNullOrEmpty(marketSpecifier))
                            payload = payload + $"<selection id=\"{fixture.FiSp}\" market_id=\"{marketMapping.MaTeId}\" outcome_id=\"{selection.SeTeId}\"/>";
                        else
                            payload = payload + $"<selection id=\"{fixture.FiSp}\" market_id=\"{marketMapping.MaTeId}\" specifiers=\"{marketSpecifier}\" outcome_id=\"{selection.SeTeId}\"/>";
                    }

                    payload = payload + $"</selections>";
                }


                var response = await client.PostAsync($"/v1/custombet/calculate", new StringContent(payload, System.Text.Encoding.UTF8, "application/xml"));

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    XmlDocument doc = new XmlDocument();

                    doc.LoadXml(result);

                    #region Validate response

                    XmlElement root = doc.DocumentElement;

                    if (root.Name != "calculation_response")
                    {
                        return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
                        {
                            StatusCode = StatusCode.No_Record_Found,
                            Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                        };
                    }

                    selectionOddsResponse.TotalOdds = 0.0m;
                    //selectionOddsResponse.Probability = 0.0m;

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

                            var marketResponse = GetBetBuilderMarketsXml(marketNodes.ChildNodes, newMarketMappings, newMarkets, newSelections, request.LanguageCode);
                            if (marketResponse.Any())
                                selectionOddsResponse.Markets.AddRange(marketResponse);
                        }
                    }


                    #endregion

                    return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString(),
                        Result = selectionOddsResponse
                    };
                }
                else
                {
                    return new BaseResponseDTO<BetBuilderMarketSelectionResponseDTO>
                    {
                        StatusCode = StatusCode.No_Record_Found,
                        Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                    };
                }
            }

            #endregion

        }

        private List<BetBuilderMarketDTO> GetBetBuilderMarketsXml(XmlNodeList list, List<MarketMappingFilter> marketMappings, List<MarketFilter> markets, List<SelectionFilter> selections, LanguageCode languageCode)
        {
            var response = new List<BetBuilderMarketDTO>();

            foreach (XmlElement marketNode in list)
            {
                var marketTemplateId = marketNode.Attributes["id"].Value;
                var specifiers = marketNode.HasAttribute("specifiers") ? marketNode.Attributes["specifiers"].Value : string.Empty;

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

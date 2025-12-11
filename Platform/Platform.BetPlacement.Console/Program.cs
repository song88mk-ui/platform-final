using Platform.Shared.Enums;
using Platform.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.BetPlacement.Console
{
    internal class Program
    {
        private static HttpClient _httpClient;
        static void Main(string[] args)
        {
            System.Console.WriteLine($"{DateTime.UtcNow}: BEGIN");

            if (_httpClient == null)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://sportsbook-gateway-nippon10-staging.playingops.com"),
                };

                _httpClient.DefaultRequestHeaders.Add("x-client-id", "A96AB8BC-2970-EB11-89FC-00224815B68B");
                _httpClient.DefaultRequestHeaders.Add("x-operator-token", "BAA4AF30-3FF5-40C9-A198-0AE0F3F396C6");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", "8LKvrh6C2fe8mKXZ");
            }

            for (int x = 0; x < 100; x++)
            {
                Thread.Sleep(10000);

                for (int i = 0; i < 10; i++)
                {
                    var liveSports = GetLiveFixtureCount().Result;

                    if (liveSports.StatusCode == StatusCode.Success)
                    {
                        foreach (var sport in liveSports.Result.Sports.Where(c => c.SportId.IsVirtualSportId()).ToList())
                        {
                            var result = GetLiveFixtures(sport.SportId).Result;

                            if (result.StatusCode == StatusCode.Success)
                            {
                                foreach (var fixture in result.Result.Fixtures)
                                {
                                    Parallel.ForEach(fixture.Markets, market =>
                                    {
                                        if (market.MarketStatusId == (int)MarketStatus.Active)
                                        {
                                            var selection = market.Selections.Where(c => c.IsActive == true && decimal.Parse(c.DecimalOdds) > 1m).First();

                                            var sw = Stopwatch.StartNew();

                                            var betResult = PlaceBet(new BetRequest
                                            {
                                                Stake = 3,
                                                OddsType = OddsType.Decimal,
                                                OddsChangeType = OddsChangeType.None,
                                                Channel = Channel.Internet,
                                                IPAddress = "112.198.252.23",
                                                IsBoosted = false,
                                                Selections = new List<BetSelectionRequest>
                                        {
                                            new BetSelectionRequest
                                            {
                                                SportId = fixture.SportId,
                                                SelectionId = selection.SelectionId,
                                                Odds = decimal.Parse(selection.DecimalOdds)
                                            }
                                        }
                                            }).Result;

                                            sw.Stop();

                                            System.Console.WriteLine($"{DateTime.UtcNow}: {sw.ElapsedMilliseconds} : {betResult.StatusCode} | SpId={fixture.SportId}, SpNa={fixture.SportName}, FiId={fixture.FixtureId}, FiNa={fixture.FixtureName}, MaId={market.MarketId}, MarketName={market.MarketName}, SeId={selection.SelectionId}, SeNa={selection.SelectionName}");
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }

            System.Console.WriteLine($"{DateTime.UtcNow}: END");
        }
        public static async Task<BaseResponseDTO<TWFixtureCountResponseDTO>> GetLiveFixtureCount()
        {
            try
            {
                var response = await _httpClient.GetAsync("v1/sportsbook/livefixturecount");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWFixtureCountResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }

        public static async Task<BaseResponseDTO<TWResponseDTO>> GetLiveFixtures(int sportId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/sportsbook/sports/livefixtures?sportId={sportId}");

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public static async Task<BaseResponseDTO<BetResponseDTO>> PlaceBet(BetRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v2/bet/placement", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<BetResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<BetResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<BetResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<BetResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }

    public class TWResponseDTO
    {
        public TWResponseDTO()
        {
            Fixtures = new List<FixtureDTO>();
        }
        public List<FixtureDTO> Fixtures { get; set; }
    }

    public class FixtureDTO
    {
        public FixtureDTO()
        {
            Markets = new List<MarketDTO>();
        }
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int SegmentId { get; set; }
        public string SegmentName { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string FixtureId { get; set; }
        public string ProviderFixtureId { get; set; }
        public int fixtureTypeId { get; set; }
        public string FixtureName { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public DateTime FixtureStartDate { get; set; }
        public bool IsLive { get; set; }
        public bool IsLiveStream { get; set; }
        public int FixtureStatusId { get; set; }
        public string FixtureStatusName { get; set; }
        public int MarketCount { get; set; }
        public string SMSCode { get; set; }
        public string StatisticsURL { get; set; }
        public string Score { get; set; }
        public string CurrentPeriod { get; set; }
        public string CurrentMinute { get; set; }
        public List<int> BetStop { get; set; }
        public bool IsBetStop { get; set; }
        public bool IsBetStopGlobal { get; set; }
        public int SortOrder { get; set; }
        public List<MarketDTO> Markets { get; set; }
    }

    public class MarketDTO
    {
        public MarketDTO()
        {
            Selections = new List<SelectionDTO>();
        }
        public string MarketTemplateId { get; set; }
        public string MarketId { get; set; }
        public string MarketName { get; set; }
        public string Line { get; set; }
        public bool IsCashout { get; set; }
        public int MarketStatusId { get; set; }
        public string MarketStatusName { get; set; }
        public List<SelectionDTO> Selections { get; set; }
    }

    public class SelectionDTO
    {
        public string SelectionTemplateId { get; set; }
        public string SelectionId { get; set; }
        public string SelectionName { get; set; }
        public string DecimalOdds { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string CorrelationId { get; set; }
    }

    public class BaseResponseDTO<T>
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }
    }



    public class BetRequest
    {
        public BetRequest()
        {
            Selections = new List<BetSelectionRequest>();
        }
        public decimal Stake { get; set; }
        public OddsType OddsType { get; set; }
        public OddsChangeType OddsChangeType { get; set; }
        public Channel Channel { get; set; }
        public string IPAddress { get; set; }
        public bool IsBoosted { get; set; } = false;
        public List<BetSelectionRequest> Selections { get; set; }
    }

    public class BetSelectionRequest
    {
        public int SportId { get; set; }
        public string SelectionId { get; set; }
        public decimal Odds { get; set; }
    }



    public class BetResponseDTO
    {
        public string UserId { get; set; }
        public string BetId { get; set; }
        public BetStatus BetStatus { get; set; }
        public decimal Stake { get; set; }
        public List<BetSelectionState> BetSelections { get; set; }
    }

    public class BetSelectionState
    {
        public StatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string SelectionId { get; set; }
        public decimal Odds { get; set; }
        public string CorrelationId { get; set; }
    }

    public class TWFixtureCountResponseDTO
    {
        public TWFixtureCountResponseDTO()
        {
            Sports = new List<FixtureCount>();
        }
        public List<FixtureCount> Sports { get; set; }
    }

    public class FixtureCount
    {
        public int SportId { get; set; }
        public string SportName { get; set; }
        public int Count { get; set; }
    }
}

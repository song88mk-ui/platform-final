using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Client.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private readonly IMTSAuthRepository _mtsAuthRepository;
        private readonly IMTSCCFRepository _mtsCCFRepository;

        private readonly PlatformContext _platformContext;

        public SportsbookRepository(HttpClient httpClient, IConfiguration configuration, IMTSAuthRepository mtsAuthRepository, IMTSCCFRepository mtsCCFRepository, PlatformContext platformContext)
        {
            _configuration = configuration;
            _mtsAuthRepository = mtsAuthRepository;
            _mtsCCFRepository = mtsCCFRepository;
            _platformContext = platformContext;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformSportsbookAPI"]);
            //_httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<BaseResponseDTO<ActiveFixturesResponseDTO>> GetActiveFixtures(string apiKey, string clientId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<ActiveFixturesResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new ActiveFixturesRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
            };

            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/activefixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixturesResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ActiveFixturesResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ActiveFixturesResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<TWLimitedResponseDTO>> GetActiveFixtureDetails(string apiKey, string clientId, string fixtureId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new TWFixtureDetailsRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                FixtureId = fixtureId,
                ClientCode = clientResponse.Result.ClientCode
            };
            try
            {
                var response = await _httpClient.PostAsync($"v2/sportsbook/fixtures/details/limited", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<TWLimitedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<TWLimitedResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<TWLimitedResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<TWLimitedResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<ActiveFixtureCountResponseDTO>> GetActiveFixtureCount(string apiKey, string clientId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<ActiveFixtureCountResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new ActiveFixtureCountRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
            };
            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/activefixtures/count", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<ActiveFixtureCountResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<ActiveFixtureCountResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<ActiveFixtureCountResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<SportsLookupResponseDTO>> GetSportsLookup(string apiKey, string clientId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new SportsLookupRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
            };

            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/lookup/sports", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SportsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SportsLookupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SportsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<SegmentsLookupResponseDTO>> GetSegmentsLookup(string apiKey, string clientId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<SegmentsLookupResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new SegmentsLookupRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
            };

            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/lookup/segments", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<SegmentsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<SegmentsLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<SegmentsLookupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<SegmentsLookupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<LeaguesLookupResponseDTO>> GetLeaguesLookup(string apiKey, string clientId, LanguageCode languageCode)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<LeaguesLookupResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var request = new LeaguesLookupRequestDTO
            {
                ClientId = clientResponse.Result.ClientId,
                LanguageCode = languageCode,
                ClientCode = clientResponse.Result.ClientCode
            };

            try
            {
                var response = await _httpClient.PostAsync($"v1/sportsbook/lookup/leagues", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<LeaguesLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<LeaguesLookupResponseDTO>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<LeaguesLookupResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<LeaguesLookupResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
        public async Task<BaseResponseDTO<CustomerConfidenceFactorResponseDTO>> GetCustomerConfidenceFactor(string apiKey, string clientId, string userId)
        {
            var clientResponse = await GetClient(clientId);

            if (clientResponse.StatusCode != StatusCode.Success)
            {
                return new BaseResponseDTO<CustomerConfidenceFactorResponseDTO>
                {
                    StatusCode = clientResponse.StatusCode,
                    Message = clientResponse.Message
                };
            }

            var result = await _platformContext.ConfidenceFactor.Where(c => c.UserId == $"{clientResponse.Result.ClientCode}{userId.Replace("-", string.Empty)}").ToListAsync();

            if (result.Count == 0)
            {
                return new BaseResponseDTO<CustomerConfidenceFactorResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.No_Record_Found.ToString().Replace('_', ' ')
                };
            }

            return new BaseResponseDTO<CustomerConfidenceFactorResponseDTO>
            {
                StatusCode = StatusCode.Success,
                Message = StatusCode.Success.ToString(),
                Result = new CustomerConfidenceFactorResponseDTO
                {
                    Values = result.Select(s => new CustomerConfidenceFactor
                    {
                        Timestamp = s.Timestamp,
                        CurrentCCF = s.CurrentCCF,
                        PreviousCCF = s.PreviousCCF,
                        IsSpecificSport = s.IsSpecificSport,
                        SportId = s.SportId,
                        SportName = s.SportName,
                        AppliedTo = s.AppliedTo
                    }).ToList()
                }
            };
        }

        private async Task<BaseResponseDTO<ClientResponse>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid == false)
            {
                return new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }

            var result = await _platformContext.Client.FindAsync(guid);

            if (result == null || result.IsActive == false)
            {
                return new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                };
            }
            else
            {
                return new BaseResponseDTO<ClientResponse>
                {
                    StatusCode = StatusCode.Success,
                    Message = StatusCode.Success.ToString().Replace('_', ' '),
                    Result = new ClientResponse
                    {
                        ClientId = result.ClientId,
                        ClientName = result.ClientName,
                        ClientCode = result.ClientCode,
                        LimitId = result.LimitId,
                        IsActive = result.IsActive,
                        Endpoint = result.Endpoint,
                        CompanyShare = result.EffectiveDate.Date < DateTime.UtcNow.Date ? result.NextCompanyShare : result.CurrentCompanyShare,
                        ClientIdRef = result.ClientIdRef
                    }
                };
            }
        }
    }

    public class ClientResponse
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public bool IsActive { get; set; }
        public string Endpoint { get; set; }
        public decimal CompanyShare { get; set; }
        public int ClientIdRef { get; set; }
    }
}

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
            var clientCode = (await GetClient(clientId)).Result;
            
            var request = new ActiveFixturesRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                ClientCode = clientCode
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
            var clientCode = (await GetClient(clientId)).Result;
            
            var request = new TWFixtureDetailsRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                FixtureId = fixtureId,
                ClientCode = clientCode
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
            var clientCode = (await GetClient(clientId)).Result;

            var request = new ActiveFixtureCountRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                ClientCode = clientCode
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
            var clientCode = (await GetClient(clientId)).Result;

            var request = new SportsLookupRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                ClientCode = clientCode
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
            var clientCode = (await GetClient(clientId)).Result;

            var request = new SegmentsLookupRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                ClientCode = clientCode
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
            var clientCode = (await GetClient(clientId)).Result;

            var request = new LeaguesLookupRequestDTO
            {
                ClientId = Guid.NewGuid(), //TODO: replace with proper validation
                LanguageCode = languageCode,
                ClientCode = clientCode
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
            var clientCode = _configuration["Custom:ClientCode"];

            if (!string.IsNullOrEmpty(clientId))
            {
                //if (clientId.ToUpper() == "5093617A-9367-EB11-89FB-00224815B68B")
                //    clientCode = "10" ;

                if (clientId.ToUpper() == "5093617A-9367-EB11-89FB-00224815B68B" || clientId.ToUpper() == "BFED6DA4-D1B5-4437-665D-08D8EE99AC1C")
                    clientCode = "10I";

                if (clientId.ToUpper() == "A96AB8BC-2970-EB11-89FC-00224815B68B" || clientId.ToUpper() == "917070EC-1DC1-4E10-28E8-08D8EE99CA8E")
                    clientCode = "10J";

                if (clientId.ToUpper() == "A15C2C99-1F0E-4C24-CD4C-08D930DC2F4E" || clientId.ToUpper() == "D59C309B-7204-40D5-A986-08D940573A8F")
                    clientCode = "10K";

                if (clientId.ToUpper() == "BC04ECBB-6A1E-414D-CD4D-08D930DC2F4E" || clientId.ToUpper() == "244F1375-7BC3-48D9-66F9-08D94057C6CB")
                    clientCode = "10G";

                if (clientId.ToUpper() == "AFD99514-CB77-4667-CD50-08D930DC2F4E" || clientId.ToUpper() == "5E777D88-645A-420E-66FA-08D94057C6CB")
                    clientCode = "10N";

                if (clientId.ToUpper() == "694CEF9F-4C35-4C7B-CD4E-08D930DC2F4E" || clientId.ToUpper() == "75DD691C-F96C-4816-66FB-08D94057C6CB")
                    clientCode = "10T";

                if (clientId.ToUpper() == "AE83C5AD-396E-4406-CD4F-08D930DC2F4E" || clientId.ToUpper() == "5245C54C-EB98-42E4-66FC-08D94057C6CB")
                    clientCode = "10Z";
            }

            var result = await _platformContext.ConfidenceFactor.Where(c => c.UserId == $"{clientCode}{userId.Replace("-", string.Empty)}").ToListAsync();

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

        private async Task<BaseResponseDTO<string>> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                var result = await _platformContext.Client.FindAsync(guid);

                if (result == null || result.IsActive == false)
                {
                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                        Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' '),
                        Result = string.Empty
                    };
                }
                else
                {
                    return new BaseResponseDTO<string>
                    {
                        StatusCode = StatusCode.Success,
                        Message = StatusCode.Success.ToString().Replace('_', ' '),
                        Result = result.ClientCode
                    };
                }
            }

            return new BaseResponseDTO<string>
            {
                StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' '),
                Result = string.Empty
            };
        }
    }
}

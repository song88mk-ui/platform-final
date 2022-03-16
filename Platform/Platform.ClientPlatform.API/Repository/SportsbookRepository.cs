using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientPlatform.API.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private readonly PlatformContext _platformContext;

        public SportsbookRepository(IConfiguration configuration, HttpClient httpClient, PlatformContext platformContext)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _platformContext = platformContext;
            _httpClient.BaseAddress = new Uri(_configuration["API:PlatformSportsbookAPI"]);
        }

        public async Task<BaseResponseDTO<TWResponseDTO>> GetTopFixtures(string clientId)
        {
            try
            {
                var guidClientId = Guid.Parse(clientId);
                var client = await _platformContext.Client.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == guidClientId);

                if (client == null)
                {
                    return new BaseResponseDTO<TWResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                    };
                }

                var request = new TWRequestDTO
                {
                    ClientId = client.ClientId,
                    ClientCode = client.ClientCode,
                    LanguageCode = LanguageCode.EN
                };

                var response = await _httpClient.PostAsync($"v2/sportsbook/topfixtures", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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
    }
}

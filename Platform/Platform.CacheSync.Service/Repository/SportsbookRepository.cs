using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.CacheSync.Service.Repository
{
    public class SportsbookRepository : ISportsbookRepository
    {
        private readonly IConfiguration _configuration;

        public SportsbookRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<BaseResponseDTO<bool>> DeleteCachePreload(CachePreloadRequestDTO request, string baseUri)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(baseUri);
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var response = await httpClient.PostAsync($"v1/sportsbook/cachepreload", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                        return JsonSerializer.Deserialize<BaseResponseDTO<bool>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    else
                    {
                        return new BaseResponseDTO<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}"
                        };
                    }
                }
            }
            catch (Exception ex) 
            {
                return new BaseResponseDTO<bool>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}

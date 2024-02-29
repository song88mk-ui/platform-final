using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
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

        public async Task<BaseResponseDTO<bool>> DeleteCachePreload(CachePreloadRequestDTO request, string baseUri, Dictionary<string, string> header = null)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(baseUri);


                    if (header != null)
                    {
                        foreach (var key in header)
                        {
                            httpClient.DefaultRequestHeaders.Add(key.Key, key.Value);
                        }
                    }
                    //httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var response = await httpClient.PostAsync($"v1/cachepreload", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

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

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
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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
                        return JsonSerializer.Deserialize<BaseResponseDTO<bool>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<bool>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')}-{response.ReasonPhrase}"
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

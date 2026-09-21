using Microsoft.Extensions.Logging;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    // Backend guardrail for BO-106 (sub-task of BO-96): reads the client's current webprops
    // feature flags (FullCashout / PartialCashout, set from BO's Client Site Config) directly
    // from webprops-api on every call. Deliberately NOT cached here - the whole point of this
    // repository is to give CashoutBet a real-time answer instead of trusting client-side state
    // (platform-web's ClientInfo.Config, kept in sync via the webprops-change SignalR relay)
    // that a caller may not have received yet, or may be bypassing entirely by hitting this API
    // directly. If webprops-api load from this becomes a concern, add a short-TTL cache (a few
    // seconds) rather than removing the check - never trust a value older than that for a
    // control that's meant to take effect immediately.
    public class WebPropsRepository : IWebPropsRepository
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly ILogger<WebPropsRepository> _logger;

        public WebPropsRepository(HttpClient httpClient, ILogger<WebPropsRepository> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<BaseResponseDTO<WebPropsConfigResponseDTO>> GetClientFeatureFlags(string clientCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/config/{clientCode.ToUpper()}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"[GetClientFeatureFlags] clientCode={clientCode} status={response.StatusCode} content={content}");

                    return new BaseResponseDTO<WebPropsConfigResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = content
                    };
                }

                var result = JsonSerializer.Deserialize<WebPropsConfigResponseDTO>(content, _jsonOptions);

                return new BaseResponseDTO<WebPropsConfigResponseDTO>
                {
                    StatusCode = StatusCode.Success,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[GetClientFeatureFlags] error clientCode={clientCode}");

                return new BaseResponseDTO<WebPropsConfigResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = ex.Message
                };
            }
        }
    }
}

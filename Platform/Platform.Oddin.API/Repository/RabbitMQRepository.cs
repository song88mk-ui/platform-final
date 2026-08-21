using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System;

namespace Platform.Oddin.API.Repository
{
    public class RabbitMQRepository : IRabbitMQRepository
    {
        // shared instance: JsonSerializerOptions caches type metadata per instance; a fresh instance per call rebuilds it every request
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public RabbitMQRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:RabbitMQAPI"]);
        }

        public BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request)
        {
            try
            {
                var response = _httpClient.PostAsync($"v1/broker/publish", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<MessageResponseDTO>>(response.Content.ReadAsStringAsync().Result, _jsonOptions);
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<MessageResponseDTO>>(response.Content.ReadAsStringAsync().Result, _jsonOptions);
                    else
                    {
                        return new BaseResponseDTO<MessageResponseDTO>
                        {
                            StatusCode = StatusCode.Error,
                            Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<MessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})"
                };
            }
        }
    }
}

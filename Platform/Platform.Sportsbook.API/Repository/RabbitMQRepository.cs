using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.Sportsbook.API.Repository
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

        public async Task<BaseResponseDTO<MessageResponseDTO>> PublishToRabbitMQAPI(MessageRequestDTO request)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/broker/publish", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<MessageResponseDTO>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                else
                {
                    return new BaseResponseDTO<MessageResponseDTO>
                    {
                        StatusCode = StatusCode.Error,
                        Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({response.ReasonPhrase})",
                        Result = new MessageResponseDTO
                        {
                            IsSuccess = false
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTO<MessageResponseDTO>
                {
                    StatusCode = StatusCode.Error,
                    Message = $"{StatusCode.Error.ToString().Replace('_', ' ')} ({ex.Message})",
                    Result = new MessageResponseDTO
                    {
                        IsSuccess = false
                    }
                };
            }
        }
    }
}

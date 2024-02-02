using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Platform.ClientSportsbook.API.Repository
{
    public class RabbitMQRepository : IRabbitMQRepository
    {
        private readonly HttpClient _httpClient;

        public RabbitMQRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public BaseResponseDTO<MessageResponseDTO> PublishViaHttp(MessageRequestDTO request)
        {
            try
            {
                var response = _httpClient.PostAsync($"v1/broker/publish", new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                    return JsonSerializer.Deserialize<BaseResponseDTO<MessageResponseDTO>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                else
                {
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        return JsonSerializer.Deserialize<BaseResponseDTO<MessageResponseDTO>>(response.Content.ReadAsStringAsync().Result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
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

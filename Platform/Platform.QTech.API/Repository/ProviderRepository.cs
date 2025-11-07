using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Platform.QTech.API.Enums;
using Platform.QTech.API.Models;

namespace Platform.QTech.API.Repository
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _userName;
        public readonly string _password;

        public ProviderRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:QTechAPI"]);

            _userName = _configuration["Custom:QTAPIUsername"];
            _password = _configuration["Custom:QTAPIPassword"];
        }
        public async Task<Tuple<AuthenticationCode, AuthenticationResponse>> GetToken()
        {
            try
            {
                var response = await _httpClient.GetAsync($"v1/auth/token?grant_type=password&response_type=token&username={_userName}&password={_password}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<AuthenticationResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new Tuple<AuthenticationCode, AuthenticationResponse>(AuthenticationCode.OK, result);
                }
                else
                {
                    var result = JsonSerializer.Deserialize<ErrorResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new Tuple<AuthenticationCode, AuthenticationResponse>(Enum.Parse<AuthenticationCode>(result.Code), new AuthenticationResponse());
                }
            }
            catch (Exception)
            {
                return new Tuple<AuthenticationCode, AuthenticationResponse>(AuthenticationCode.PROVIDER_ERROR, new AuthenticationResponse());
            }
        }
        public async Task<AuthTokenRevokeCode> RevokeToken(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.PostAsync($"v1/auth/token", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return AuthTokenRevokeCode.OK;
                }
                else
                {
                    var result = JsonSerializer.Deserialize<ErrorResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return Enum.Parse<AuthTokenRevokeCode>(result.Code);
                }
            }
            catch (Exception)
            {
                return AuthTokenRevokeCode.PROVIDER_ERROR;
            }
        }
        public async Task<Tuple<GameLobbyCode, GameLobbyResponse>> GetGameLobbyURL(GameLobbyRequest request)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.Token}");

                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                var payload = JsonSerializer.Serialize(request, serializeOptions);

                var response = await _httpClient.PostAsync($"v1/games/lobby-url", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GameLobbyResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    return new Tuple<GameLobbyCode, GameLobbyResponse>(GameLobbyCode.OK, result);
                }
                else
                {
                    var result = JsonSerializer.Deserialize<ErrorResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    var t1 = JsonSerializer.Serialize(result, serializeOptions);

                    return new Tuple<GameLobbyCode, GameLobbyResponse>(Enum.Parse<GameLobbyCode>(result.Code), new GameLobbyResponse());
                }
            }
            catch (Exception)
            {
                return new Tuple<GameLobbyCode, GameLobbyResponse>(GameLobbyCode.PROVIDER_ERROR, new GameLobbyResponse());
            }
        }
    }
}

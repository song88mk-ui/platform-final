using Platform.API.Data;
using Platform.API.Data.Entities;
using Platform.API.Models;
using Platform.Shared.Enums;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.API.Repository
{
    public class PlatformRepository : IPlatformRepository
    {
        private readonly HttpClient _httpClient;
        private readonly PlatformContext _platformContext;

        public PlatformRepository(HttpClient httpClient, PlatformContext platformContext)
        {
            _httpClient = httpClient;
            _platformContext = platformContext;
        }
        public async Task<ClientResponse> GetClient(string clientId)
        {
            var isGuid = Guid.TryParse(clientId, out Guid guid);

            if (isGuid)
            {
                var result = await _platformContext.Client.FindAsync(guid);

                if (result == null) return null;
                if (result.IsActive == false) return null;

                return new ClientResponse
                {
                    ClientId = result.ClientId,
                    ClientName = result.ClientName,
                    ClientCode = result.ClientCode,
                    LimitId = result.LimitId,
                    IsActive = result.IsActive,
                    Endpoint = result.Endpoint
                };
            }

            else
                return null;
        }
        public async Task<AuthenticationResponse> Authenticate(string operatorToken, string endpoint)
        {
            if (string.IsNullOrEmpty(operatorToken) || string.IsNullOrWhiteSpace(operatorToken)) return null;

            _httpClient.DefaultRequestHeaders.Add("x-operator-token", operatorToken);

            return await JsonSerializer.DeserializeAsync<AuthenticationResponse>
                (await _httpClient.GetStreamAsync($"{endpoint}/authentication"), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }
        public async Task<WalletBalanceResponse> GetWalletBalance(decimal balance)
        {
            return new WalletBalanceResponse
            {
                Balance = balance
            };
        }
    }
}

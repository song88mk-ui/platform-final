using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;

namespace Platform.Oddin.API.Repository
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public ProviderRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_configuration["API:OddinAPI"]);
        }
    }
}

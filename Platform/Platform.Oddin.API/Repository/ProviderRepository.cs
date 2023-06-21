using Microsoft.Extensions.Configuration;
using Platform.Oddin.API.Enums;
using Platform.Oddin.API.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

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

using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net;

namespace Platform.Sportsbook.API.Repository
{
    public class BetBuilderRepository : IBetBuilderRepository
    {
        private readonly HttpClient _httpClient;
        public BetBuilderRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.betradar.com");
            _httpClient.DefaultRequestHeaders.Add("x-access-token", "wLCaM7vc80CADU33rz");
        }

        public async Task<Tuple<HttpStatusCode, string>> ValidateBetBuilderCombination(string payload)
        {
            try
            {
                var response = await _httpClient.PostAsync($"v1/custombet/calculate-filter", new StringContent(payload, System.Text.Encoding.UTF8, "application/xml"));

                return new Tuple<HttpStatusCode, string>(response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}

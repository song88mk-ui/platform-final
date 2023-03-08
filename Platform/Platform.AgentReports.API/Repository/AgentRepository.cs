using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Platform.AgentReports.API.Models.Response;
using Platform.Shared.Data.Reports;
using Platform.Shared.Models;
using System.Net.Http;

namespace Platform.AgentReports.API.Repository
{
    public class AgentRepository : IAgentRepository
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        #endregion

        #region CTOR
        public AgentRepository(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }
        #endregion

        #region Methods
        public BaseResponseDTO<UserDetailsResponse> GetUserDetails(long userId)
        {
            var response = _httpClient.GetAsync($"{_config["AgentUrl"]}api/v3/user/details/{userId}").Result;

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<BaseResponseDTO<UserDetailsResponse>>(response.Content
                    .ReadAsStringAsync().Result);
            }

            return new BaseResponseDTO<UserDetailsResponse>();
        }

        #endregion
    }
}

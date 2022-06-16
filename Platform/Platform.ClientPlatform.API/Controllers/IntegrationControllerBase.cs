using Microsoft.AspNetCore.Mvc;

namespace Platform.ClientPlatform.API.Controllers
{
    public class IntegrationControllerBase : ControllerBase
    {
        public string ClientId { get; set; }
        public string ApiKey { get; set; }
        public string CountryCode { get; set; }
        public string LanguageCode { get; set; }
        public string OperatorToken { get; set; }
        public string IPAddress { get; set; }
    }
}

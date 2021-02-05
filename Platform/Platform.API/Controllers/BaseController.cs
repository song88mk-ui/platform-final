using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Enums;

namespace Platform.API.Controllers
{
    [ApiController, EnableCors("AllowAllOrigins")]
    public class BaseController : ControllerBase
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string LimitId { get; set; }
        public string Endpoint { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public string OperatorToken { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public CurrencyCode CurrencyCode { get; set; }
        public string IPAddress { get; set; }
    }
}
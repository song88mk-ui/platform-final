using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Enums;
using System;

namespace Platform.ClientSportsbook.API.Controllers
{
    public class IntegrationControllerBase : ControllerBase
    {
        public Guid ClientId { get; set; }
        public string CountryCode { get; set; }
        public LanguageCode LanguageCode { get; set; }
        public string OperatorToken { get; set; }
        public string IPAddress { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.ClientPlatform.API.Controllers;
using Platform.Shared.Enums;
using System.Linq;

namespace Platform.ClientPlatform.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public APIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var clientId = context.HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            if (string.IsNullOrEmpty(clientId))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                });
                return;
            }

            ((IntegrationControllerBase)context.Controller).ClientId = clientId;
            ((IntegrationControllerBase)context.Controller).ApiKey = _configuration["Custom:PlatformAPIKey"];
            ((IntegrationControllerBase)context.Controller).CountryCode = context.HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).OperatorToken = context.HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).IPAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).LanguageCode = string.IsNullOrEmpty(context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();
        }
    }
}

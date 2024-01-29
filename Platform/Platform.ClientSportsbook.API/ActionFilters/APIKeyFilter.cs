using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.ClientSportsbook.API.Controllers;
using Platform.Shared.Enums;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Platform.ClientSportsbook.API.ActionFilters
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
            var isGuid = Guid.TryParse(clientId, out Guid clientGuid);

            if (string.IsNullOrEmpty(clientId) || !isGuid)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers.ToString().Replace('_', ' ')
                });
                return;
            }

            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey) || _configuration["Custom:APIKey"] != apiKey)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Not_Authorized,
                    Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                });
                return;
            }

            ((IntegrationControllerBase)context.Controller).ClientId = clientGuid;
            ((IntegrationControllerBase)context.Controller).CountryCode = context.HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).OperatorToken = context.HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).IPAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            var languageCode = string.IsNullOrEmpty(context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();
            ((IntegrationControllerBase)context.Controller).LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), languageCode);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Platform.ClientSportsbook.API.Controllers;
using Platform.ClientSportsbook.API.Models;
using Platform.Shared.Data.Platform;
using Platform.Shared.Enums;
using System;
using System.Linq;

namespace Platform.ClientSportsbook.API.ActionFilters
{
    public class ApiKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private readonly PlatformContext _platformContext;
        private readonly IMemoryCache _cache;

        public ApiKeyFilter(IConfiguration configuration, PlatformContext platformContext, IMemoryCache cache)
        {
            _configuration = configuration;
            _platformContext = platformContext;
            _cache = cache;
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

            var clientCode = GetClientCodeById(clientGuid);
            if (string.IsNullOrEmpty(clientCode))
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
                    StatusCode = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers.ToString().Replace('_', ' ')
                });
                return;
            }

            var languageCode = string.IsNullOrEmpty(context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();
            var isValidLanguageCode = Enum.IsDefined(typeof(LanguageCode), languageCode);

            if (!isValidLanguageCode)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Unsupported_LanguageCode,
                    Message = StatusCode.Unsupported_LanguageCode.ToString().Replace('_', ' ')
                });
                return;
            }

            ((IntegrationControllerBase)context.Controller).ClientKeyFilter = new ClientKeyFilter()
            {
                ClientId = clientGuid,
                ClientCode = clientCode,
                IpCountryCode = context.HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault(),
                LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), languageCode),
                IpAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            };
        }

        private string GetClientCodeById(Guid clientId)
        {
            var key = $"cc:{clientId.ToString().ToUpper()}";

            if (_cache.TryGetValue(key, out string clientCode))
                if (clientCode != null)
                    return clientCode;

            var result = _platformContext.Client.Find(clientId);

            if (result != null && result.IsActive)
            {
                _cache.Set(key, result.ClientCode, TimeSpan.FromHours(24));

                return result.ClientCode;
            }

            return string.Empty;
        }
    }
}

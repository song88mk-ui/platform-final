using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.ClientSportsbook.API.Controllers;
using Platform.Shared.Enums;
using System.Collections.Generic;
using System;
using System.Linq;
using Platform.ClientSportsbook.API.Models;
using System.Threading.Tasks;
using Platform.Shared.Data.Platform;
using Platform.Shared.Utility;

namespace Platform.ClientSportsbook.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private readonly PlatformContext _platformContext;
        private readonly IMemCache _memCache;

        public APIKeyFilter(IConfiguration configuration, PlatformContext platformContext, IMemCache memCache)
        {
            _configuration = configuration;
            _platformContext = platformContext;
            _memCache = memCache;
        }

        public override async void OnActionExecuting(ActionExecutingContext context)
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

            var cliendCode = await GetClientCodeById(clientGuid);
            if (string.IsNullOrEmpty(cliendCode))
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
            var ipAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).ClientKeyFilter = new ClientKeyFilter()
            {
                ClientId = clientGuid,
                ClientCode = cliendCode,
                IPCountryCode = context.HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault(),
                LanguageCode = (LanguageCode)Enum.Parse(typeof(LanguageCode), languageCode)
            };
        }

        private async Task<string> GetClientCodeById(Guid clientId)
        {
            var key = $"cc:{clientId.ToString().ToUpper()}";

            var cache = _memCache.GetCache<string>(key);

            if (cache != null)
                return cache;

            var result = await _platformContext.Client.FindAsync(clientId);

            if (result != null && result.IsActive)
            {
                _memCache.AddOrUpdateCache(key, result.ClientCode, 24);

                return result.ClientCode;
            }

            return string.Empty;
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.API.Controllers;
using Platform.API.Models;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System;
using System.Linq;

namespace Platform.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private readonly OrientalGamingConfigDTO _ogConfig;

        public APIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
            var ogConfig = configuration["Custom:OrientalGaming"];
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || (_configuration["Custom:APIKey"] != apiKey && _configuration["Custom:WebSocketAPIKey"] != apiKey))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers.ToString().Replace('_', ' ')
                });

                return;
            }

            if (_configuration["Custom:WebSocketAPIKey"] == apiKey)
                return;

            var ogClientIds = _configuration["Custom:OrientalGaming:ClientIds"].Split(',');
            var clientId = context.HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            if (ogClientIds.Contains(clientId.ToUpper()))
            {
                var isSbLaunch = context.HttpContext.Request.Headers["x-og-issblaunch"].FirstOrDefault();
                var languageCode = string.IsNullOrEmpty(context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "en" : context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToLower();

                ((BaseController)context.Controller).OGParameters = new OGClientRequest
                {
                    PlayerCode = context.HttpContext.Request.Headers["x-og-player-code"].FirstOrDefault(),
                    GameCode = context.HttpContext.Request.Headers["x-og-game-code"].FirstOrDefault(),
                    PoolId = int.Parse(context.HttpContext.Request.Headers["x-og-pool-id"].FirstOrDefault()),
                    BetLimit = int.Parse(context.HttpContext.Request.Headers["x-og-bet-limit"].FirstOrDefault()),
                    Nickname = context.HttpContext.Request.Headers["x-og-nickname"].FirstOrDefault(),
                    Timestamp = long.Parse(context.HttpContext.Request.Headers["x-og-timestamp"].FirstOrDefault()),
                    Signature = context.HttpContext.Request.Headers["x-og-signature"].FirstOrDefault(),
                    IsSBLaunch = isSbLaunch != null ? bool.Parse(isSbLaunch) : false,
                    CurrencyCode = context.HttpContext.Request.Headers["x-og-currency"].FirstOrDefault(),
                    LanguageCode = languageCode,
                };
            }
        }
    }
}

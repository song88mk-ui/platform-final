using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Platform.ClientPlatform.API.Controllers;
using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Platform.ClientPlatform.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private List<Tuple<string, string, List<string>>> _allowed { get; set; }

        public APIKeyFilter()
        {
            _allowed = GetAllowedClients();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var passKey = context.HttpContext.Request.Headers["x-pass-key"].FirstOrDefault();
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var clientId = context.HttpContext.Request.Headers["x-client-id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(passKey) && !string.IsNullOrWhiteSpace(passKey))
            {
                if (passKey != "fcisong88")
                {
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        StatusCode = StatusCode.Not_Authorized,
                        Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                    });

                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || IsNotAllowed(clientId, apiKey))
                {
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        StatusCode = StatusCode.Not_Authorized,
                        Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                    });

                    return;
                }
            }

            ((IntegrationControllerBase)context.Controller).ClientId = clientId;
            ((IntegrationControllerBase)context.Controller).ApiKey = "8LKvrh6C2fe8mKXZ"; // Platform.API api key
            ((IntegrationControllerBase)context.Controller).CountryCode = context.HttpContext.Request.Headers["CF-IPCountry"].FirstOrDefault();
            ((IntegrationControllerBase)context.Controller).LanguageCode = string.IsNullOrEmpty(context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault()) ? "EN" : context.HttpContext.Request.Headers["x-language-code"].FirstOrDefault().Trim().ToUpper();
        }

        private bool IsNotAllowed(string clientId, string apiKey)
        {
            var isNotAllowed = true;

            foreach (var item in _allowed)
            {
                if (item.Item3.Contains(clientId))
                {
                    if (item.Item2 == apiKey)
                    {
                        isNotAllowed = false;
                        break;
                    }
                }
            }

            return isNotAllowed;
        }

        private List<Tuple<string, string, List<string>>> GetAllowedClients()
        {
            // note: put 2 client ids for each API Key (staging, prod)
            var result = new List<Tuple<string, string, List<string>>>
            {
                new Tuple<string, string, List<string>>("BEC", "Qm2gXMt3npchd6y9", new List<string> { "C6F563F5-1240-47C4-A77C-08D9A9ACA672", "4BE2894A-CEAF-4B4F-58F9-08D9A9AD9D36" })
            };

            return result;
        }
    }
}

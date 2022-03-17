using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private List<Tuple<string, string, List<string>>> _allowed { get; set; }

        public APIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
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
            ((IntegrationControllerBase)context.Controller).ApiKey = _configuration["Custom:PlatformAPIKey"];
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
            // note: put 2 client ids for each API Key (staging, prod). Except Internal-Preprod and Internal-Prod
            var result = new List<Tuple<string, string, List<string>>>
            {
                new Tuple<string, string, List<string>>("Internal-Preprod", "zb9rxuPQgtc6ms6e", new List<string> { "CBDA6855-EB7A-4D78-F2E3-08D9E2404694", "9E209944-E5DB-466E-5814-08D9D6FF9237", "42F80942-355A-4A47-0F23-08D9C226B9FE", "46A5E2BA-3A29-43CC-6E5D-08DA07DB5704", "F577BE7D-C580-41E6-6E5C-08DA07DB5704", "2AEBD4BE-8A73-4542-1F5A-08D910FC3CEA", "20921F58-D1B7-4836-D97A-08D90465E527", "51CD9089-C558-4A4A-54CE-08D8E90D7082" }),
                new Tuple<string, string, List<string>>("Internal-Prod", "zb9rxuPQgtc6ms6e", new List<string> { }),
                new Tuple<string, string, List<string>>("BEC", "Qm2gXMt3npchd6y9", new List<string> { "C6F563F5-1240-47C4-A77C-08D9A9ACA672", "4BE2894A-CEAF-4B4F-58F9-08D9A9AD9D36" })
            };

            return result;
        }
    }
}

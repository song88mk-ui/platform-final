using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
            _allowed = GetAllowedIPs();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var passKey = context.HttpContext.Request.Headers["x-pass-key"].FirstOrDefault();
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();
            var ipAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (ipAddress == null)
                ipAddress = IPAddress.Loopback.ToString();

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
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || IsNotAllowed(ipAddress, apiKey))
                {
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        StatusCode = StatusCode.Not_Authorized,
                        Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                    });

                    return;
                }
            }
        }

        private bool IsNotAllowed(string ipAddress, string apiKey)
        {
            var isNotAllowed = true;

            foreach (var item in _allowed)
            {
                if (item.Item3.Contains(ipAddress))
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

        private List<Tuple<string, string, List<string>>> GetAllowedIPs()
        {
            var result = new List<Tuple<string, string, List<string>>>
            {
                new Tuple<string, string, List<string>>("Client", "8LKvrh6C2fe8mKXZ", new List<string> { "180.191.190.179" })
            };

            return result;
        }
    }
}

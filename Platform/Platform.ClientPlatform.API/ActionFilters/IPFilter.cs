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
    public class IPFilter : ActionFilterAttribute
    {
        private List<Tuple<string, List<string>>> _allowed = new List<Tuple<string, List<string>>>();

        public IPFilter()
        {
            _allowed = GetAllowedIPs();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ipAddress = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (ipAddress == null)
                ipAddress = IPAddress.Loopback.ToString();

            if (IsNotAllowed(ipAddress))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    StatusCode = StatusCode.Not_Authorized,
                    Message = StatusCode.Not_Authorized.ToString().Replace('_', ' ')
                });
                return;
            }
            ((IntegrationControllerBase)context.Controller).IPAddress = ipAddress;
        }

        private bool IsNotAllowed(string ipAddress)
        {
            var isNotAllowed = true;

            foreach (var item in _allowed)
            {
                if (item.Item2.Contains(ipAddress))
                {
                    isNotAllowed = false;
                    break;
                }
            }

            return isNotAllowed;
        }

        private List<Tuple<string, List<string>>> GetAllowedIPs()
        {
            var result = new List<Tuple<string, List<string>>>
            {
                new Tuple<string, List<string>>("Internal-Preprod", new List<string> { "127.0.0.1", "112.198.204.225", "84.17.47.126", "43.250.225.226" }),
                new Tuple<string, List<string>>("Internal-Prod", new List<string> { }),
                new Tuple<string, List<string>>("BEC", new List<string> { })
            };

            return result;
        }
    }
}

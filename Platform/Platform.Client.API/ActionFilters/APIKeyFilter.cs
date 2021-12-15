using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Platform.Client.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private List<Tuple<string, string, List<string>>> _allowed = new List<Tuple<string, string, List<string>>>();

        public APIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
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
                new Tuple<string, string, List<string>>("Lino", "hK3UUgRrTz5rZ5mP", new List<string> { "180.191.190.179" }),
                new Tuple<string, string, List<string>>("10BET-dev", "8LKvrh6C2fe8mKXZ", new List<string> { "34.77.0.160", "104.199.39.234", "35.241.253.22", "104.248.44.186" }),
                new Tuple<string, string, List<string>>("10BET-STAGING", "8LKvrh6C2fe8mKXZ", new List<string> { "34.76.197.66", "34.140.193.159","34.77.84.232", "104.155.9.8", "104.155.35.27", "34.78.253.228", "34.76.95.115", "34.77.23.130", "35.205.226.112", "35.240.95.123", "35.205.202.76", "34.77.250.196", "34.77.123.20", "35.195.197.184", "34.76.16.81"}),
                new Tuple<string, string, List<string>>("10BET", "8LKvrh6C2fe8mKXZ", new List<string> { "34.105.187.86", "35.197.228.236", "34.105.228.11", "35.200.240.0", "34.93.179.235", "34.93.30.117", "35.187.221.222", "34.84.241.22", "34.85.72.185", "104.248.44.186", "34.146.64.164", "34.146.187.163", "34.85.67.226" }),
                new Tuple<string, string, List<string>>("10BET-AFRICA", "8LKvrh6C2fe8mKXZ", new List<string> { "35.240.49.107", "35.205.254.128", "35.195.49.214", "34.140.177.107", "35.195.98.44", "35.240.114.148", "34.140.137.252", "34.79.56.65", "34.78.0.133", "34.79.243.200", "34.140.159.68", "34.140.177.33", "35.240.11.211", "104.155.32.49", "34.79.31.223", "34.76.75.142", "35.189.240.144", "34.76.254.13", "34.140.247.195", "34.78.159.48", "35.205.12.224", "34.140.89.227", "34.76.212.139", "34.77.58.246" }),
                new Tuple<string, string, List<string>>("lolTRICKEDu", "aYKn7A2SmxzageU8", new List<string> { "15.206.238.120", "13.127.42.155" }),
                new Tuple<string, string, List<string>>("casinomobiltbankid", "j6uh5E2W74neCXrv", new List<string> { "111.118.215.154" }),
                new Tuple<string, string, List<string>>("BilalAlim", "2J8DtUe8ugT3wM7x", new List<string> { "34.93.140.109", "35.244.36.155" }),
                new Tuple<string, string, List<string>>("betrallyindia", "44MRNepFKUr6tzp9", new List<string> { "67.227.164.101", "67.227.164.102", "67.227.164.103", "104.27.154.210", "104.27.155.210", "172.67.170.132" }),
                new Tuple<string, string, List<string>>("riviera", "nq737zHSdqyu9VFY", new List<string> { "54.228.183.109", "93.171.241.132", "176.37.75.85", "54.195.248.219" }),
                new Tuple<string, string, List<string>>("sportspunter", "TTFjKkXJsDke4ubu", new List<string> { "207.228.252.8", "207.228.252.24" }),
                new Tuple<string, string, List<string>>("fresh8", "LsPuZzpzeQu4ea64", new List<string> { "146.148.121.95", "35.240.70.112", "146.148.122.135", "35.234.151.134" }),
                new Tuple<string, string, List<string>>("mediabuy", "XGsRbcyBk3zy8byZ", new List<string> { "185.45.66.20", "185.45.67.120" }),
                new Tuple<string, string, List<string>>("oddsscannerin", "ZCB4GjAVF6jbpmWP", new List<string> { "99.79.63.184", "3.97.8.231", "83.240.158.106", "94.63.95.190", "119.94.9.193" }),
                new Tuple<string, string, List<string>>("ankushban92", "2mGAjkUSAMheK4hc", new List<string> { "122.161.183.149", "182.68.56.189" })
            };

            return result;
        }
    }
}

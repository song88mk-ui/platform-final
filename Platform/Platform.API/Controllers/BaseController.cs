using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Platform.API.Models;
using System;
using System.Linq;

namespace Platform.API.Controllers
{
    public class BaseController : ControllerBase
    {
        public OGClientRequest OGParameters { get; set; }

        protected string ClientIPv4 => GetClientIPAddress(HttpContext);
        protected string ClientIPv6 => GetClientIPAddress(HttpContext, preferIPv6: true);
        protected string ClientCountry => GetClientCountry(HttpContext);

        protected string GetClientCountry(HttpContext context)
        {
            // support internal x-country-code header passed from platform web
            var xcc = HttpContext.Request.Headers["x-country-code"].ToString();
            if (!string.IsNullOrEmpty(xcc))
                return xcc;

            // rely on cf
            var cfCountry = HttpContext.Request.Headers["CF-IPCountry"].ToString();
            if (!string.IsNullOrEmpty(cfCountry))
                return cfCountry;
            return ""; // default
        }

        protected string GetClientIPAddress(HttpContext context, bool preferIPv6 = false)
        {
            var cfConnectingIpv6 = context.Request.Headers["Cf-Connecting-IPv6"].ToString();
            if (preferIPv6 && !string.IsNullOrEmpty(cfConnectingIpv6))
                return cfConnectingIpv6;

            var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].ToString();
            if (!string.IsNullOrEmpty(cfConnectingIp))
                return cfConnectingIp;

            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                var ipAddresses = xForwardedFor.Split(',').Select(ip => ip.Trim())
                        .Where(ip => !string.IsNullOrEmpty(ip) && ip != "127.0.0.1").ToList();
                if (ipAddresses.Any())
                    return ipAddresses.First().Split(':').First();
            }

            const string kubePrefix = "::ffff:";

            var remoteIp = context.Connection?.RemoteIpAddress?.ToString();

            if (!string.IsNullOrEmpty(remoteIp))
            {
                if (remoteIp.StartsWith(kubePrefix))
                        return remoteIp.Substring(kubePrefix.Length);
                return remoteIp;
            }

            return "127.0.0.1"; // fallback
        }
    }



}

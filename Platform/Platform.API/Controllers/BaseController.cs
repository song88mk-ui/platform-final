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

        // helper for getting Remote Caller Country Code by either supplied
        // from X-Country-Code or from Cloudflare's CF-IPCountry
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
            /* no default, it would be easier to debug something you know its not working
             * rather than having a default value that is working but not what you intend */
        }

        /* Helper to obtain the Remote IP Address of the Caller with the following:
         *
         * 1. Header (X-User-IP)          - If supplied (eg. Platform Web)
         * 2. Header (CF-Connecting-IPv6) - from Cloudflare if Client is IPv6
         * 3. Header (CF-Connecting-IP)   - from Cloudflare if Client is IPV4
         * 4. Header (X-Forwarded-For)    - Standard header for proxies
         * 5. Remote IP                   - From remote socket connection
         *
         * Params:
         *      preferXUserIp = false (if you want to ignore the x-user-ip header)
         *      preferIPv6 = false    (if you want to get the ipv4 only address)
         */
        protected string GetClientIPAddress(HttpContext context, bool preferIPv6 = false, bool preferXUserIp = true)
        {
            var xUserIp = context.Request.Headers["x-user-ip"].ToString();
            if (preferXUserIp && !string.IsNullOrEmpty(xUserIp))
                return xUserIp;

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

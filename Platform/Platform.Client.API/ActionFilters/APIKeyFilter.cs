using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using System.Linq;

namespace Platform.Client.API.ActionFilters
{
    public class APIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public APIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || _configuration["Custom:APIKey"] != apiKey)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers,
                    Message = StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers.ToString().Replace('_', ' ')
                });
                return;
            }
        }
    }
}

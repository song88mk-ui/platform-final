using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using System.Linq;

namespace Platform.API.ActionFilters
{
    public class InternalAPIKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public InternalAPIKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var apiKey = context.HttpContext.Request.Headers["x-api-key"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey) || _configuration["Custom:InternalAPIKey"] != apiKey)
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

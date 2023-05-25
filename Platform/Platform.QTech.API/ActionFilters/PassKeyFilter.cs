using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Platform.Shared.Enums;
using System.Linq;

namespace Platform.QTech.API.ActionFilters
{
    public class PassKeyFilter : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;

        public PassKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var passKey = context.HttpContext.Request.Headers["Pass-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(passKey) || string.IsNullOrWhiteSpace(passKey) || _configuration["Custom:PassKey"] != passKey)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Code = "LOGIN_FAILED",
                    Message = "Incorrect Pass-Key in request headers"
                });
                return;
            }
        }
    }
}

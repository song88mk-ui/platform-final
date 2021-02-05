using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Platform.API.Controllers;
using Platform.API.Repository;
using Platform.Shared.Enums;
using System.Linq;

namespace Platform.API.ActionFilters
{
    public class WalletActionFilter : ActionFilterAttribute
    {
        private readonly IPlatformRepository _platformRepository;

        public WalletActionFilter(IPlatformRepository platformRepository)
        {
            _platformRepository = platformRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var clientId = context.HttpContext.Request.Headers["x-client-id"].FirstOrDefault();
            var operatorToken = context.HttpContext.Request.Headers["x-operator-token"].FirstOrDefault();

            var client = _platformRepository.GetClient(clientId).Result;

            if (client == null)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    StatusCode = (int)ResultType.MissingUnsupportedClientId,
                    Message = "Missing/Unsupported x-client-id in Request Headers"
                });
                return;
            }

            var authentication = _platformRepository.Authenticate(operatorToken, client.Endpoint).Result;

            if (authentication == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    StatusCode = (int)ResultType.MissingUnsupportedOperatorToken,
                    Message = "Missing/Unsupported x-operator-token in Request Headers"
                });
                return;
            }

            ((BaseController)context.Controller).Balance = authentication.Balance;
        }
    }
}

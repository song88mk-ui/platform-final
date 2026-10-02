using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Platform.API.ActionFilters;
using Platform.API.Controllers;
using Platform.Shared.Enums;

namespace Platform.API.Test.ActionFilters
{
    // Covers GM-873. APIKeyFilter called clientId.ToUpper() on the x-client-id header with no null
    // check, so a request with a valid x-api-key and no x-client-id threw NullReferenceException and
    // the caller got a bare 500. In PRD that was about 960 errors a day across /v1/sportsbook/*,
    // /v1/bet/config and /v1/wallet/balance. These tests pin that a missing or blank client id is now
    // a 400 with StatusCode 102, that the API key check still runs first, that the WebSocket key
    // still skips the client id check, and that non-OG and OG client ids behave as before.
    public class APIKeyFilterTests
    {
        private const string APIKey = "test-api-key";
        private const string WebSocketAPIKey = "test-websocket-api-key";
        private const string OGClientId = "OG-CLIENT";

        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Custom:APIKey"] = APIKey,
                    ["Custom:WebSocketAPIKey"] = WebSocketAPIKey,
                    ["Custom:OrientalGaming:ClientIds"] = OGClientId,
                })
                .Build();

        private static ActionExecutingContext BuildContext(IDictionary<string, string> headers, BaseController controller)
        {
            var httpContext = new DefaultHttpContext();
            foreach (var header in headers)
                httpContext.Request.Headers[header.Key] = header.Value;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller);
        }

        private static StatusCode ReadStatusCode(IActionResult? result)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value!;
            return (StatusCode)value.GetType().GetProperty("StatusCode")!.GetValue(value)!;
        }

        [Fact]
        public void Missing_client_id_with_valid_api_key_returns_400_with_client_id_status_code()
        {
            var controller = new BaseController();
            var context = BuildContext(new Dictionary<string, string> { ["x-api-key"] = APIKey }, controller);

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Equal(StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers, ReadStatusCode(context.Result));
            Assert.Null(controller.OGParameters);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_client_id_with_valid_api_key_returns_400_with_client_id_status_code(string clientId)
        {
            var context = BuildContext(new Dictionary<string, string> { ["x-api-key"] = APIKey, ["x-client-id"] = clientId }, new BaseController());

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Equal(StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers, ReadStatusCode(context.Result));
        }

        [Fact]
        public void Missing_client_id_and_wrong_api_key_returns_api_key_status_code()
        {
            var context = BuildContext(new Dictionary<string, string> { ["x-api-key"] = "wrong" }, new BaseController());

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Equal(StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers, ReadStatusCode(context.Result));
        }

        [Fact]
        public void WebSocket_api_key_without_client_id_passes()
        {
            var context = BuildContext(new Dictionary<string, string> { ["x-api-key"] = WebSocketAPIKey }, new BaseController());

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void Non_og_client_id_passes_without_og_parameters()
        {
            var controller = new BaseController();
            var context = BuildContext(new Dictionary<string, string> { ["x-api-key"] = APIKey, ["x-client-id"] = "other-client" }, controller);

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Null(context.Result);
            Assert.Null(controller.OGParameters);
        }

        [Fact]
        public void Og_client_id_in_any_case_populates_og_parameters()
        {
            var controller = new BaseController();
            var context = BuildContext(new Dictionary<string, string>
            {
                ["x-api-key"] = APIKey,
                ["x-client-id"] = OGClientId.ToLower(),
                ["x-og-player-code"] = "player",
                ["x-og-game-code"] = "game",
                ["x-og-pool-id"] = "7",
                ["x-og-bet-limit"] = "100",
                ["x-og-nickname"] = "nick",
                ["x-og-timestamp"] = "1759363200",
                ["x-og-signature"] = "sig",
                ["x-og-currency"] = "USD",
            }, controller);

            new APIKeyFilter(BuildConfiguration()).OnActionExecuting(context);

            Assert.Null(context.Result);
            Assert.NotNull(controller.OGParameters);
            Assert.Equal("player", controller.OGParameters.PlayerCode);
            Assert.Equal(7, controller.OGParameters.PoolId);
            Assert.Equal("en", controller.OGParameters.LanguageCode);
        }
    }
}

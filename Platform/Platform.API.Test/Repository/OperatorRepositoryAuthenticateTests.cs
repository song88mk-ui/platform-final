using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Models;
using Platform.API.Repository;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using System.Net;
using System.Net.Http;

namespace Platform.API.Test.Repository
{
    // OperatorRepository.AuthenticatePlayer reads the operator /v1/authentication response. Most
    // operators send "balance" as a JSON number; OG-backed wallets send it as a string, which failed
    // bet placement with "Operator Error (The JSON value could not be converted to System.Decimal.
    // Path: $.balance ...)" (GM-649). These tests drive the real repository through Authenticate
    // (request: null routes to AuthenticatePlayer), so reverting its deserialization options fails them.
    public class OperatorRepositoryAuthenticateTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public int Calls { get; private set; }

            public StubHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content)
                });
            }
        }

        private sealed class CapturingLogger : ILogger<OperatorRepository>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Entries.Add((logLevel, formatter(state, exception)));
        }

        private static Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string responseBody)
        {
            var repository = new OperatorRepository(
                new HttpClient(new StubHandler(HttpStatusCode.OK, responseBody)),
                new ConfigurationBuilder().Build(),
                NullLogger<OperatorRepository>.Instance);

            return repository.Authenticate("client-1", "operator-token", "http://operator.test", false, null!);
        }

        private static async Task<(BaseResponseDTO<AuthenticationResponseDTO> Response, StubHandler Handler, CapturingLogger Logger)> AuthenticateWithEndpoint(string? endpoint, OGClientRequest? request)
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"statusId\":1000,\"balance\":1}");
            var logger = new CapturingLogger();
            var repository = new OperatorRepository(new HttpClient(handler), new ConfigurationBuilder().Build(), logger);

            var response = await repository.Authenticate("client-1", "operator-token", endpoint!, false, request!);
            return (response, handler, logger);
        }

        [Fact]
        public async Task Authenticate_NumericBalance_ReturnsSuccessUnchanged()
        {
            var response = await Authenticate("{\"userId\":\"u1\",\"statusId\":1000,\"balance\":1234.56,\"payable\":10.5,\"currencyCode\":\"ETB\",\"languageCode\":\"en\"}");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(1234.56m, response.Result!.Balance);
            Assert.Equal(10.5m, response.Result.Payable);
            Assert.Equal(1000, response.Result.StatusId);
            Assert.Equal("u1", response.Result.UserId);
            Assert.Equal("ETB", response.Result.CurrencyCode);
        }

        [Fact]
        public async Task Authenticate_StringBalance_ReturnsSuccessForOGBackedWallets()
        {
            var response = await Authenticate("{\"userId\":\"u1\",\"statusId\":1000,\"balance\":\"1234.56\",\"payable\":\"0\",\"currencyCode\":\"ETB\",\"languageCode\":\"en\"}");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(1234.56m, response.Result!.Balance);
            Assert.Equal(0m, response.Result.Payable);
        }

        [Fact]
        public async Task Authenticate_NullBalance_ReturnsOperatorError()
        {
            // Deliberately unchanged: a null balance on a success response is still an operator error,
            // not silently read as a 0 balance.
            var response = await Authenticate("{\"statusId\":1000,\"balance\":null}");

            Assert.Equal(StatusCode.Operator_Error, response.StatusCode);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task Authenticate_NonNumericStringBalance_ReturnsOperatorError()
        {
            var response = await Authenticate("{\"statusId\":1000,\"balance\":\"abc\"}");

            Assert.Equal(StatusCode.Operator_Error, response.StatusCode);
            Assert.Null(response.Result);
        }

        // An active client with an empty Client.Endpoint (STG client BLA) made new Uri(endpoint) throw
        // UriFormatException before AuthenticatePlayer's try block, so platform-api returned a bare 500.
        // A missing or unusable endpoint must come back as Operator_Error (mapped to 400 by the
        // controllers) without calling the operator.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("not-a-url")]
        public async Task Authenticate_EmptyOrInvalidEndpoint_ReturnsOperatorErrorWithoutCallingOperator(string? endpoint)
        {
            var (response, handler, logger) = await AuthenticateWithEndpoint(endpoint, null);

            Assert.Equal(StatusCode.Operator_Error, response.StatusCode);
            Assert.Equal("Operator Error (Operator endpoint is not configured for this client)", response.Message);
            Assert.Null(response.Result);
            Assert.Equal(0, handler.Calls);

            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Contains("client-1", entry.Message);
            Assert.DoesNotContain("operator-token", entry.Message);
        }

        [Fact]
        public async Task Authenticate_OGPath_EmptyEndpoint_ReturnsOperatorErrorWithoutCallingOperator()
        {
            // No Custom:OrientalGaming section, so the guard must run before _ogConfig is read.
            var (response, handler, logger) = await AuthenticateWithEndpoint("", new OGClientRequest { PlayerCode = "p1" });

            Assert.Equal(StatusCode.Operator_Error, response.StatusCode);
            Assert.Equal("Operator Error (Operator endpoint is not configured for this client)", response.Message);
            Assert.Null(response.Result);
            Assert.Equal(0, handler.Calls);
            Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

            public StubHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content)
                });
        }

        private static Task<BaseResponseDTO<AuthenticationResponseDTO>> Authenticate(string responseBody)
        {
            var repository = new OperatorRepository(
                new HttpClient(new StubHandler(HttpStatusCode.OK, responseBody)),
                new ConfigurationBuilder().Build(),
                NullLogger<OperatorRepository>.Instance);

            return repository.Authenticate("client-1", "operator-token", "http://operator.test", false, null!);
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
    }
}

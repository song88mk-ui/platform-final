using Microsoft.Extensions.Logging.Abstractions;
using Platform.API.Repository;
using Platform.Shared.Enums;
using System.Net;
using System.Net.Http;

namespace Platform.API.Test.Repository
{
    // WebPropsRepository is the BO-106 guardrail's only HTTP boundary (Platform.API/Repository/
    // WebPropsRepository.cs). These tests pin the request URL it builds from whatever key it is
    // given, and its Success/fail-closed handling - not whether that key is the client GUID or
    // ClientCode, which is the call site's responsibility and is covered in
    // CashoutBetWebPropsGuardrailTests.
    public class WebPropsRepositoryTests
    {
        private sealed class RecordingHandler : HttpMessageHandler
        {
            public Uri? RequestUri { get; private set; }
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public RecordingHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content)
                });
            }
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                throw new HttpRequestException("simulated network failure");
        }

        private static WebPropsRepository CreateRepository(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://platform-webprops-api:3000")
            };

            return new WebPropsRepository(httpClient, NullLogger<WebPropsRepository>.Instance);
        }

        [Fact]
        public async Task GetClientFeatureFlags_BuildsUrlFromGivenKey_Uppercased()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, "{\"Features\":{\"FullCashout\":true,\"PartialCashout\":false}}");
            var repository = CreateRepository(handler);

            await repository.GetClientFeatureFlags("ffdecdb4-10a7-4aec-9d93-40dec796ce9b");

            Assert.Equal("/api/v1/config/FFDECDB4-10A7-4AEC-9D93-40DEC796CE9B", handler.RequestUri!.AbsolutePath);
        }

        [Fact]
        public async Task GetClientFeatureFlags_Success_ParsesFeatures()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, "{\"Features\":{\"FullCashout\":true,\"PartialCashout\":false}}");
            var repository = CreateRepository(handler);

            var response = await repository.GetClientFeatureFlags(Guid.NewGuid().ToString());

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.NotNull(response.Result?.Features);
            Assert.True(response.Result!.Features.FullCashout);
            Assert.False(response.Result.Features.PartialCashout);
        }

        [Fact]
        public async Task GetClientFeatureFlags_NonSuccessStatus_ReturnsError()
        {
            var handler = new RecordingHandler(HttpStatusCode.NotFound, "{\"message\":\"no properties for client\"}");
            var repository = CreateRepository(handler);

            var response = await repository.GetClientFeatureFlags(Guid.NewGuid().ToString());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task GetClientFeatureFlags_HttpFailure_ReturnsErrorInsteadOfThrowing()
        {
            var repository = CreateRepository(new ThrowingHandler());

            var response = await repository.GetClientFeatureFlags(Guid.NewGuid().ToString());

            Assert.Equal(StatusCode.Error, response.StatusCode);
        }

        [Fact]
        public async Task GetClientFeatureFlags_BaseAddressNotConfigured_FailsClosedWithoutCallingHttp()
        {
            // Startup.cs only sets BaseAddress when API:WebProps:Uri is configured (guarding
            // against the unconditional `new Uri("")` that used to crash every PlatformRepository
            // resolve). This is the runtime shape that guard produces: no BaseAddress at all.
            var handler = new RecordingHandler(HttpStatusCode.OK, "{}");
            var unconfigured = new WebPropsRepository(new HttpClient(handler), NullLogger<WebPropsRepository>.Instance);

            var response = await unconfigured.GetClientFeatureFlags(Guid.NewGuid().ToString());

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Null(handler.RequestUri);
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Shared.Enums;
using Platform.Shared.Models;
using Platform.Shared.Models.Provider.BetGenius;
using Platform.Shared.Providers.BGLiveStream;
using Platform.Shared.Repositories;

// DeliveriesDto and DeliveryProviderDto are declared in both Platform.Shared.Models and
// Platform.Shared.Models.Provider.BetGenius. BGLiveStreamDto.Ot is the BetGenius one, so the
// aliases pin that rather than relying on using-order to disambiguate.
using BgDeliveries = Platform.Shared.Models.Provider.BetGenius.DeliveriesDto;
using BgDelivery = Platform.Shared.Models.Provider.BetGenius.DeliveryProviderDto;

namespace Platform.Shared.Test.Providers
{
    // Covers BetGeniusLiveStream.GetLiveStream, which turns a fixture's stored live-stream JSON
    // into a playable token for one viewer's device and region.
    //
    // Every failure path returns string.Empty rather than throwing, including the outer catch, so
    // a caller cannot tell "no stream for your region" from "BetGenius is down" from "the stored
    // JSON is malformed". The tests below pin each route to that same empty result, which is what
    // makes the behaviour auditable: if one of them starts throwing instead, a test says so.
    //
    // The region and device check is the security-adjacent one: Pe holds the permitted
    // "device:region" pairs, and a viewer outside them must never receive a token.
    public class BetGeniusLiveStreamTests
    {
        private sealed class FakeBetGeniusRepository : IBetGeniusRepository
        {
            public BaseResponseDTO<BGAuth> AuthResponse { get; set; } = new()
            {
                StatusCode = StatusCode.Success,
                Result = new BGAuth { IdToken = "id-token", ExpiresIn = 3600 }
            };

            public BaseResponseDTO<BGLiveStreamResponseDto>? TokenResponse { get; set; } = new()
            {
                StatusCode = StatusCode.Success,
                Result = new BGLiveStreamResponseDto { Url = "https://stream", Token = "stream-token" }
            };

            public int AuthCalls { get; private set; }
            public int TokenCalls { get; private set; }
            public (string FixtureId, string LiveStreamId, string DeliveryId, string Region, string Device)? LastTokenRequest { get; private set; }

            public Task<BaseResponseDTO<BGAuth>> GetBetGeniusAuthorizationToken()
            {
                AuthCalls++;
                return Task.FromResult(AuthResponse);
            }

            public Task<BaseResponseDTO<BGLiveStreamResponseDto>> GetLiveStreamToken(string token, string fixtureId, string liveStreamId, string deliveryId, string region, string device)
            {
                TokenCalls++;
                LastTokenRequest = (fixtureId, liveStreamId, deliveryId, region, device);
                return Task.FromResult(TokenResponse!);
            }

            public Task<BaseResponseDTO<BGEvent>> GetBetGeniusFixture(string token, string fixtureId) =>
                throw new NotImplementedException("GetBetGeniusFixture is not called by BetGeniusLiveStream");
        }

        private static BetGeniusLiveStream CreateProvider(FakeBetGeniusRepository repo)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

            return new BetGeniusLiveStream(configuration, NullLogger<BetGeniusLiveStream>.Instance, repo);
        }

        private static string LiveStreamJson(string permitted = "web:gb", string streamId = "stream-1", string deliveryId = "delivery-1") =>
            JsonSerializer.Serialize(new List<BGLiveStreamDto>
            {
                new()
                {
                    Id = streamId,
                    Pe = new List<string> { permitted },
                    Ot = new BgDeliveries { Hls = new List<BgDelivery> { new() { Id = deliveryId } } }
                }
            });

        [Fact]
        public void ChannelType_IsBetGenius()
        {
            Assert.Equal(ChannelType.BG, CreateProvider(new FakeBetGeniusRepository()).ChannelType);
        }

        [Fact]
        public async Task GetLiveStream_PermittedDeviceAndRegion_ReturnsTheSerialisedToken()
        {
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson(), "gb", "web");

            Assert.Contains("stream-token", result);
            Assert.Equal(1, repo.TokenCalls);
            Assert.Equal("fixture-1", repo.LastTokenRequest!.Value.FixtureId);
            Assert.Equal("stream-1", repo.LastTokenRequest!.Value.LiveStreamId);
            Assert.Equal("delivery-1", repo.LastTokenRequest!.Value.DeliveryId);
        }

        [Theory]
        [InlineData("WEB", "GB")]
        [InlineData("Web", "Gb")]
        public async Task GetLiveStream_DeviceAndRegionMatchIsCaseInsensitive(string device, string region)
        {
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson("web:gb"), region, device);

            Assert.NotEqual(string.Empty, result);
        }

        [Fact]
        public async Task GetLiveStream_RegionNotPermitted_ReturnsEmptyWithoutAuthenticating()
        {
            // The geo restriction is enforced before any credential is spent, so a blocked viewer
            // never reaches BetGenius at all.
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson("web:gb"), "de", "web");

            Assert.Equal(string.Empty, result);
            Assert.Equal(0, repo.AuthCalls);
            Assert.Equal(0, repo.TokenCalls);
        }

        [Fact]
        public async Task GetLiveStream_OneOfSeveralPermittedPairsMatches_ReturnsTheToken()
        {
            // Pe is a list of permitted "device:region" pairs and only one needs to match. With a
            // single-entry list "any match" and "all match" are indistinguishable, so this case
            // is what proves the check is an Any and not an All.
            var repo = new FakeBetGeniusRepository();

            var json = JsonSerializer.Serialize(new List<BGLiveStreamDto>
            {
                new()
                {
                    Id = "stream-1",
                    Pe = new List<string> { "mobile:de", "web:gb" },
                    Ot = new BgDeliveries { Hls = new List<BgDelivery> { new() { Id = "delivery-1" } } }
                }
            });

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", json, "gb", "web");

            Assert.Contains("stream-token", result);
        }

        [Fact]
        public async Task GetLiveStream_DeviceNotPermitted_ReturnsEmpty()
        {
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson("web:gb"), "gb", "mobile");

            Assert.Equal(string.Empty, result);
            Assert.Equal(0, repo.TokenCalls);
        }

        [Fact]
        public async Task GetLiveStream_EmptyStreamList_ReturnsEmpty()
        {
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", "[]", "gb", "web");

            Assert.Equal(string.Empty, result);
            Assert.Equal(0, repo.AuthCalls);
        }

        [Fact]
        public async Task GetLiveStream_MalformedJson_ReturnsEmptyRatherThanThrowing()
        {
            // The outer catch swallows the JsonException. Pinned because the caller has no other
            // signal that the stored fixture data is corrupt.
            var repo = new FakeBetGeniusRepository();

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", "not json", "gb", "web");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLiveStream_AuthFails_ReturnsEmptyWithoutRequestingAToken()
        {
            var repo = new FakeBetGeniusRepository
            {
                AuthResponse = new BaseResponseDTO<BGAuth> { StatusCode = StatusCode.Error, Message = "denied" }
            };

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson(), "gb", "web");

            Assert.Equal(string.Empty, result);
            Assert.Equal(0, repo.TokenCalls);
        }

        [Fact]
        public async Task GetLiveStream_TokenCallErrors_ReturnsEmpty()
        {
            var repo = new FakeBetGeniusRepository
            {
                TokenResponse = new BaseResponseDTO<BGLiveStreamResponseDto> { StatusCode = StatusCode.Error, Message = "upstream" }
            };

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson(), "gb", "web");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLiveStream_RegionRejectedUpstream_ReturnsEmpty()
        {
            // BetGenius can refuse the region even after the local Pe check passed, so the two
            // guards are not redundant.
            var repo = new FakeBetGeniusRepository
            {
                TokenResponse = new BaseResponseDTO<BGLiveStreamResponseDto>
                {
                    StatusCode = StatusCode.Livestream_Is_Not_Available_In_Your_Region
                }
            };

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson(), "gb", "web");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLiveStream_ResponseWithoutAToken_ReturnsEmpty()
        {
            // A success status with no token is still no stream. Without this guard the caller
            // would receive a serialised object carrying a null token.
            var repo = new FakeBetGeniusRepository
            {
                TokenResponse = new BaseResponseDTO<BGLiveStreamResponseDto>
                {
                    StatusCode = StatusCode.Success,
                    Result = new BGLiveStreamResponseDto { Url = "https://stream", Token = null }
                }
            };

            var result = await CreateProvider(repo).GetLiveStream("fixture-1", LiveStreamJson(), "gb", "web");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetLiveStream_NoHlsDelivery_StillRequestsWithAnEmptyDeliveryId()
        {
            // A missing HLS entry is not treated as a failure: the call goes out with an empty
            // delivery id and BetGenius decides.
            var repo = new FakeBetGeniusRepository();

            var json = JsonSerializer.Serialize(new List<BGLiveStreamDto>
            {
                new()
                {
                    Id = "stream-1",
                    Pe = new List<string> { "web:gb" },
                    Ot = new BgDeliveries { Hls = new List<BgDelivery>() }
                }
            });

            await CreateProvider(repo).GetLiveStream("fixture-1", json, "gb", "web");

            Assert.Equal(string.Empty, repo.LastTokenRequest!.Value.DeliveryId);
        }

        [Fact]
        public async Task GetLiveStream_MultipleStreams_UsesTheFirst()
        {
            var repo = new FakeBetGeniusRepository();

            var json = JsonSerializer.Serialize(new List<BGLiveStreamDto>
            {
                new()
                {
                    Id = "first",
                    Pe = new List<string> { "web:gb" },
                    Ot = new BgDeliveries { Hls = new List<BgDelivery> { new() { Id = "d1" } } }
                },
                new()
                {
                    Id = "second",
                    Pe = new List<string> { "web:gb" },
                    Ot = new BgDeliveries { Hls = new List<BgDelivery> { new() { Id = "d2" } } }
                }
            });

            await CreateProvider(repo).GetLiveStream("fixture-1", json, "gb", "web");

            Assert.Equal("first", repo.LastTokenRequest!.Value.LiveStreamId);
        }
    }
}

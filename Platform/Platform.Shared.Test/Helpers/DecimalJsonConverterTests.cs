using System.Text.Json;
using Platform.Shared.Helpers;
using Xunit;

namespace Platform.Shared.Test.Helpers
{
    // Covers the tolerant Read added for operator wallet responses: operators answer error
    // results (e.g. statusId 1002 Invalid Token on /v1/bet/settlement) with "balance": null,
    // which crashed Platform.Operator.Servicev2 with "The JSON value could not be converted
    // to System.Decimal. Path: $.balance" and looped the bet in the settlement retry flow.
    public class DecimalJsonConverterTests
    {
        private class OperatorResponse
        {
            public int StatusId { get; set; }
            public decimal Balance { get; set; }
        }

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new DecimalJsonConverter() }
        };

        [Fact]
        public void Read_NullBalance_ReturnsZero()
        {
            var result = JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1002,\"balance\":null}", Options)!;

            Assert.Equal(1002, result.StatusId);
            Assert.Equal(0m, result.Balance);
        }

        [Fact]
        public void Read_NumericBalance_ReturnsValue()
        {
            var result = JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1000,\"balance\":123.456789}", Options)!;

            Assert.Equal(123.456789m, result.Balance);
        }

        [Fact]
        public void Read_StringBalance_ParsesInvariantCulture()
        {
            var result = JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1000,\"balance\":\"98.76\"}", Options)!;

            Assert.Equal(98.76m, result.Balance);
        }

        [Fact]
        public void Read_EmptyStringBalance_ReturnsZero()
        {
            var result = JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1000,\"balance\":\"\"}", Options)!;

            Assert.Equal(0m, result.Balance);
        }

        [Fact]
        public void Read_NonNumericStringBalance_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1000,\"balance\":\"abc\"}", Options));
        }

        [Fact]
        public void Read_NonNumericStringBalance_NamesTheOffendingValue()
        {
            // The message carries the value that failed. Without it, a bad operator response is
            // reported as an anonymous conversion error and nobody can tell what arrived.
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<OperatorResponse>("{\"statusId\":1000,\"balance\":\"abc\"}", Options));

            Assert.Contains("abc", exception.Message);
        }

        [Fact]
        public void Write_TrimsTrailingZeros()
        {
            var json = JsonSerializer.Serialize(1.990000000000m, Options);

            Assert.Equal("1.99", json);
        }
    }
}

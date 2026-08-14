using System;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Platform.Shared.Helpers
{
    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        // Operator wallet APIs answer error results (e.g. statusId 1002 Invalid Token) with
        // "balance": null, and some send numbers as JSON strings; read both instead of throwing.
        public override bool HandleNull => true;

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return 0m;

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();

                if (string.IsNullOrWhiteSpace(value))
                    return 0m;

                if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;

                throw new JsonException($"The JSON value \"{value}\" could not be converted to System.Decimal.");
            }

            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(decimal.Parse(value.ToString("G29"))); // Trim trailing zeros
        }
    }
}

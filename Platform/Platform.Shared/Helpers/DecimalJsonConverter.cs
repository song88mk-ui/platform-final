using Platform.Shared.Enums;
using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Platform.Shared.Helpers
{
    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(decimal.Parse(value.ToString("G29"))); // Trim trailing zeros
        }
    }
}

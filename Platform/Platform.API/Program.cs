using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Platform.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public class CustomDateTimeConverter : JsonConverter<DateTime>
        {
            private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ"; // Specify the date and time format

            // Serialize the DateTime value
            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                string formattedDateTime = value.ToString(DateTimeFormat);
                writer.WriteStringValue(formattedDateTime);
            }

            // Deserialize the DateTime value
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string dateTimeString = reader.GetString();
                return DateTime.Parse(dateTimeString);
            }
        }
    }
}

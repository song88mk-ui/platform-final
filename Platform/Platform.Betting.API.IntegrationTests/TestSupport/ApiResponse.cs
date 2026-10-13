using Platform.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Platform.Betting.API.IntegrationTests.TestSupport
{
    public static class ApiResponse
    {
        // AddControllers() in Startup keeps the System.Text.Json web defaults: camelCase names,
        // enums as numbers.
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public static async Task<(HttpResponseMessage Http, BaseResponseDTO<JsonElement> Body)> PostAsync(
            HttpClient client, string path, object request)
        {
            var response = await client.PostAsJsonAsync(path, request, Json);
            var body = await response.Content.ReadFromJsonAsync<BaseResponseDTO<JsonElement>>(Json);
            return (response, body!);
        }
    }
}

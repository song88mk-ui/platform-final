using System.Threading.Tasks;
using System;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Platform.ClientSportsbook.API.Repository
{
    public class RedisCache
    {
        Lazy<ConnectionMultiplexer> _connection;
        ConnectionMultiplexer _connectionV2;
        public RedisCache(IConfiguration configuration)
        {
            _connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(configuration["ConnectionStrings:Redis"]));
        }

        public async Task<TVal> GetAsync<TVal>(string key) where TVal : class
        {
            var database = _connection.Value.GetDatabase();
            var result = await database.StringGetAsync(key.ToLower());

            return result.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TVal>(result);
        }
    }

}
using System.Threading.Tasks;
using System;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Rewrite;

namespace Platform.ClientSportsbook.API.Repository
{
    public class RedisCache
    {
        private ConnectionMultiplexer _connection;
        private readonly IDatabase redisDb;

        public RedisCache(IConfiguration configuration)
        {
            _connection = ConnectionMultiplexer.Connect(configuration["ConnectionStrings:Redis"]);
            redisDb = _connection.GetDatabase();
        }

        public async Task<TVal> GetAsync<TVal>(string key) where TVal : class
        {
            if(redisDb == null)
            {
                return null;
            }
            var result = await redisDb.StringGetAsync(key.ToLower());

            return result.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TVal>(result);
        }
    }

}
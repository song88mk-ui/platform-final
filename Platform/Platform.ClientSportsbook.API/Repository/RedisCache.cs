using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Platform.ClientSportsbook.API.Repository
{
    public class RedisCache
    {
        private readonly Dictionary<string, IDatabase> redisDbs;

        public RedisCache(IConfiguration configuration)
        {
            var redisInstances = configuration.GetSection("ConnectionStrings:RedisInstances")
                                .Get<Dictionary<string, string>[]>()
                                .SelectMany(i => i)
                                .Select(x => new { x.Key, x.Value })
                                .ToList();
            foreach (var item in redisInstances)
            {
                if (redisDbs == null)
                    redisDbs = new Dictionary<string, IDatabase>();

                if (!redisDbs.ContainsKey(item.Key.ToUpper()))
                    redisDbs[item.Key.ToUpper()] = ConnectionMultiplexer.Connect(item.Value).GetDatabase();
            }
        }

        public async Task<TVal> GetAsync<TVal>(string clientCode, string key) where TVal : class
        {
            if (redisDbs == null)
                return null;

            if (redisDbs.TryGetValue(clientCode.ToUpper(), out IDatabase redisDb))
            {
                var result = await redisDb.StringGetAsync(key.ToLower());

                return result.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TVal>((string)result);
            }

            return null;

        }
    }

}

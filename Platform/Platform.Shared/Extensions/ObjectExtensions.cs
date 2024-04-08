using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Platform.Shared.Extensions
{
    public static class ObjectExtensions
    {
        public static IDictionary<string, object> ToDictionary(this object model, bool toLower = false)
        {
            var jsonFormat = model.ToJsonFormat(toLower);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonFormat) ?? new Dictionary<string, object>();
        }

        public static string ToJsonFormat(this object model, bool toLower = false)
        {
            return toLower ? JsonSerializer.Serialize(model).ToLower() : JsonSerializer.Serialize(model);
        }

        public static string GetDataString(this List<string> list)
        {
            if (list == null)
                return "";

            var sb = new StringBuilder();

            foreach (var key in list.OrderBy(p => p))
            {
                sb.AppendFormat("{0}&", key);
            }

            return sb.ToString().TrimEnd('&');
        }

        public static string ToSignOG(this object model, string privateKey)
        {
            var dictionary = model.ToDictionary(false);
            var list = dictionary.Where(c => c.Key != "signature")
                .Select(s => $"{s.Key}={s.Value}")
                .ToList();

            var hash = list.GetDataString();

            var strHash = hash.ToMD5Hash(privateKey).ToLower();

            return strHash;
        }

    }
}

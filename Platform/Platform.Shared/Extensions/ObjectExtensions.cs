using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static string ToSignOG(this object model, string privateKey)
        {
            var dictionary = model.ToDictionary(false);
            var list = dictionary.Where(c => c.Value != null && c.Key != "signature")
                .Select(s => $"{s.Key}={s.Value.ConvertObjectToString()}")
                .ToList();

            var hash = list.GetDataString();
            var strHash = hash.ToMD5Hash(privateKey).ToLower();
            return strHash;
        }
        public static object NormalizeEmptyStrings(object obj)
        {
            if (obj == null)
                return null;

            // string
            if (obj is string str)
                return string.IsNullOrWhiteSpace(str) ? null : str;

            // primitive/value types
            var type = obj.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime))
                return obj;

            // collections
            if (obj is IEnumerable enumerable && !(obj is IDictionary))
            {
                var list = new List<object>();

                foreach (var item in enumerable)
                {
                    list.Add(NormalizeEmptyStrings(item));
                }

                return list;
            }

            // complex object
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                var value = prop.GetValue(obj);
                var normalized = NormalizeEmptyStrings(value);

                prop.SetValue(obj, normalized);
            }

            return obj;
        }
        public static string ToStringOG(this object model)
        {
            var dictionary = model.ToDictionary(false);
            var list = dictionary.Where(c => c.Value != null && c.Key != "signature")
                .Select(s => $"{s.Key}={s.Value.ConvertObjectToString()}")
                .ToList();

            var hash = list.GetDataString();
            return hash;
        }
        public static string ToSignOG(this string stringData, string privateKey)
        {
            return stringData.ToMD5Hash(privateKey).ToLower();
        }

        private static string ConvertObjectToString(this object? obj)
        {
            if (obj != null && (((JsonElement)obj).ValueKind == JsonValueKind.Object || ((JsonElement)obj).ValueKind == JsonValueKind.Array))
                return obj.ToString().Replace("/", @"\/").Replace("\"\"", "null");
            else
                return obj?.ToString() ?? "";
        }
    }
}
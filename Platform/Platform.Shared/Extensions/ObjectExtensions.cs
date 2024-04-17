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
                .Select(s => $"{s.Key}={s.Value?.ToString().Replace("/", @"\/") ?? "null"}")
                .ToList();

            //var dictionary = ConvertObjectToDictionary(model);
            //var list = dictionary.Where(c => c.Key != "signature")
            //.Select(s => $"{s.Key}={ConvertObjectToKeyValue(s.Value).Replace("/", @"\/")}")
            //.ToList();

            var hash = list.GetDataString();
            var strHash = hash.ToMD5Hash(privateKey).ToLower();
            return strHash;
        }

        //private static string ConvertObjectToKeyValue(object obj)
        //{
        //    if (obj is object && obj.GetType().IsGenericType)
        //        return JsonSerializer.Serialize(new[] { obj });
        //    else
        //        return obj.ToString();
        //}

        //private static Dictionary<string, object> ConvertObjectToDictionary(object obj)
        //{
        //    var dict = new Dictionary<string, object>();

        //    if (obj is IEnumerable<object> enumerable && !(obj is string))
        //    {
        //        // If the object is an enumerable (e.g., a list or an array) of non-string objects,
        //        // recursively convert each item to a dictionary
        //        var mergedDictionary = new Dictionary<string, object>();
        //        foreach (var item in enumerable)
        //        {
        //            var itemDictionary = ConvertObjectToDictionary(item);
        //            foreach (var kvp in itemDictionary)
        //            {
        //                mergedDictionary[kvp.Key] = kvp.Value;
        //            }
        //        }
        //        return mergedDictionary;
        //    }

        //    if (obj is object && !obj.GetType().IsClass)
        //    {
        //        // If the object is an anonymous object, convert its properties into a dictionary
        //        var props = obj.GetType().GetProperties()
        //            .ToDictionary(
        //                prop =>
        //                {
        //                    var jsonPropertyNameAttribute = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
        //                        .FirstOrDefault() as JsonPropertyNameAttribute;
        //                    return jsonPropertyNameAttribute != null ? jsonPropertyNameAttribute.Name : prop.Name;
        //                },
        //                prop => prop.GetValue(obj)
        //            );
        //        return props;
        //    }

        //    // If the object is a class (but not string), recursively convert it to a dictionary
        //    foreach (var prop in obj.GetType().GetProperties())
        //    {
        //        var jsonPropertyNameAttribute = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
        //                        .FirstOrDefault() as JsonPropertyNameAttribute;
        //        var propName = jsonPropertyNameAttribute != null ? jsonPropertyNameAttribute.Name : prop.Name;

        //        object propValue = prop.GetValue(obj);

        //        if (propValue != null && prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
        //        {
        //            dict[propName] = ConvertObjectToDictionary(propValue);
        //        }
        //        else if (propValue is IEnumerable<object> innerEnumerable && !(propValue is string))
        //        {
        //            // If the property is a non-string enumerable, recursively convert each item to a dictionary
        //            dict[propName] = innerEnumerable.Select(ConvertObjectToDictionary).ToList();
        //        }
        //        else
        //        {
        //            // Otherwise, add the property value to the dictionary
        //            dict[propName] = propValue;
        //        }
        //    }

        //    return dict;
        //}

        public static string ConvertToUnicodeEscape(this string input)
        {
            return input.Replace("+", @"\u002B");
        }
    }
}

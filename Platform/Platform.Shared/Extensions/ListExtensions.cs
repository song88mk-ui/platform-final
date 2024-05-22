using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Platform.Shared.Extensions
{
    public static class ListExtensions
    {
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
    }
}

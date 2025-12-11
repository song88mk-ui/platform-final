using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Platform.Shared.Extensions
{
    public static class IntExtensions
    {
        public static bool IsVirtualSportId(this int sportId)
        {
            var listOfVirtualSports = new List<int>();
            //todo: append virtual sports here

            return listOfVirtualSports.Contains(sportId);
        }
    }
}

using System.Collections.Generic;

namespace Platform.Shared.Extensions
{
    public static class IntExtensions
    {
        public static List<int> GetVirtualSportIds()
        {
            return new List<int>(); //todo: append virtual sports here
        }

        public static bool IsVirtualSportId(this int sportId)
        {
            return GetVirtualSportIds().Contains(sportId);
        }

        public static bool IsESports(this int sportId)
        {
            var listOfESports = new List<int>();
            //todo: append E-Sports here

            return listOfESports.Contains(sportId);
        }
    }
}

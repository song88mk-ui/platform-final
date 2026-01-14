using System.Collections.Generic;

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

        public static bool IsESports(this int sportId)
        {
            var listOfESports = new List<int>();
            //todo: append E-Sports here

            return listOfESports.Contains(sportId);
        }
    }
}

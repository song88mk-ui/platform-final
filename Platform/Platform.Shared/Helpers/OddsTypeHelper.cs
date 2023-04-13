using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Helpers
{
    public static class OddsTypeHelper
    {
        public static decimal GetOddsByOddsType(int oddsTypeId, decimal odds, bool isBinary)
        {
            var retval = odds;

            switch (oddsTypeId)
            {
                case (int)OddsType.Malaysian:
                    retval = GetMalayOdds(odds, isBinary);
                    break;
                case (int)OddsType.HongKong:
                    retval = GetHongkongOdds(odds, isBinary);
                    break;
                case (int)OddsType.Indonesian:
                    retval = GetIndoOdds(odds, isBinary);
                    break;
                default:
                    retval = odds;
                    break;
            };

            return retval;
        }
        private static decimal GetMalayOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            if (odds <= 2.00m)
                return odds - 1m;

            var retval = -1m / (odds - 1m);

            return Math.Truncate(retval * 100) / 100;
        }

        private static decimal GetHongkongOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            return odds - 1m;
        }

        private static decimal GetIndoOdds(decimal odds, bool isBinary)
        {
            // add binary condition if more than 2 value is decimal
            if (isBinary == false)
                return odds;

            if (odds == 1m)
                return 0;

            if (odds >= 2.00m)
                return odds - 1m;

            var retval = -1m / (odds - 1m);

            return Math.Truncate(retval * 100) / 100;
        }
    }
}

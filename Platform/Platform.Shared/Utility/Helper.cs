using Platform.Shared.Enums;
using System;

namespace Platform.Shared.Utility
{
    public class Helper : IHelper
    {
        public string GetInstanceName(string providerFixtureId)
        {
            if (string.IsNullOrEmpty(providerFixtureId) || providerFixtureId.Length < 2)
                throw new ArgumentException("Invalid fixture id");

            int last = providerFixtureId[^1] - '0';
            int secondLast = providerFixtureId[^2] - '0';

            int baseDigit = (last / 2) * 2; // 0,2,4,6,8

            int result = secondLast % 2 == 0
                ? baseDigit
                : baseDigit + 1;

            return result.ToString("D2");
        }

        public static VoidFactor GetVoidFactor(BetStatus status)
        {
            var voidFactor = VoidFactor.Zero;
            if (status == BetStatus.Won || status == BetStatus.Lost)
                return VoidFactor.Zero;
            if (status == BetStatus.Void)
                return VoidFactor.One;
            if (status == BetStatus.HalfWon || status == BetStatus.HalfLost)
                return VoidFactor.Half;
            return voidFactor;
        }
        public static VoidFactor GetVoidFactor(SettlementStatus status)
        {
            var voidFactor = VoidFactor.Zero;
            if (status == SettlementStatus.Won || status == SettlementStatus.Lost)
                return VoidFactor.Zero;
            if (status == SettlementStatus.Void)
                return VoidFactor.One;
            if (status == SettlementStatus.HalfWon || status == SettlementStatus.HalfLost)
                return VoidFactor.Half;
            return voidFactor;
        }
    }
}

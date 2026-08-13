using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Enums;
using Platform.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static decimal GetStake(List<BetSelection> selections, decimal taxedStake, decimal trueStake)
        {
            var totalSelections = selections.Count();
            var nonEffectiveSelections = selections.Count(c =>
                c.BetSelectionStatusId == (int)BetStatus.Cancelled || c.BetSelectionStatusId == (int)BetStatus.Void);

            if (nonEffectiveSelections == totalSelections || selections.Any(c => c.BetSelectionStatusId == (int)BetStatus.Lost))
                return trueStake;

            //apply for Won, HalfWon, HalfLost (at least one genuine win-adjacent outcome present)
            return trueStake - taxedStake;
        }

        public static decimal GetPayout(List<Tuple<BetStatus, decimal, decimal, bool>> selections, decimal trueStake, decimal stake, bool isBetBuilder, decimal odds, OddsType oddsType)
        {
            var values = new List<decimal>();

            if (selections.Count == 1 && OddsTypeHelper.GetOddsByOddsType((int)oddsType, selections.First().Item2, selections.First().Item4) < 0 && (oddsType == OddsType.Indonesian || oddsType == OddsType.Malaysian))
            {
                switch (selections.First().Item1)
                {
                    case BetStatus.Lost:
                        return 0m * trueStake;

                    case BetStatus.HalfWon:
                        return (stake / 2) + trueStake;

                    case BetStatus.HalfLost:
                        return 0.5m * trueStake;

                    case BetStatus.Won:
                    case BetStatus.Running:
                        return trueStake + stake;

                    case BetStatus.Void:
                        return 1m * trueStake;

                    case BetStatus.Cancelled:
                        return 1m * trueStake;
                }
            }

            if (isBetBuilder == true)
            {
                if (selections.Count() == selections.Where(s => s.Item1 == BetStatus.Won || s.Item1 == BetStatus.Running).Count())
                    return (odds * trueStake);
                else if (selections.Any(s => s.Item1 == BetStatus.Lost))
                    return 0m;
                else if (selections.Any(s => s.Item1 == BetStatus.Void))
                    return trueStake;
                else if (selections.Any(s => s.Item1 == BetStatus.Cancelled))
                    return trueStake;
                else
                    return trueStake;
            }

            foreach (var selection in selections)
            {
                switch (selection.Item1)
                {
                    case BetStatus.Lost:
                        values.Add(0m);
                        break;
                    case BetStatus.HalfWon:
                        values.Add((selection.Item2 + 1) / 2);
                        break;
                    case BetStatus.HalfLost:
                        values.Add(0.5m);
                        break;
                    case BetStatus.Running:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Won:
                        values.Add(selection.Item2 * selection.Item3);
                        break;
                    case BetStatus.Void:
                        values.Add(1m);
                        break;
                    case BetStatus.Cancelled:
                        values.Add(1m);
                        break;
                }
            }

            return values.Aggregate((a, b) => a * b) * trueStake;
        }
    }
}

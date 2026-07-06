using System;
using System.Collections.Generic;

namespace Platform.Sportsbook.API.Utility
{
    // Ports bet-genius OddsFeedRepository.GetSelectionName so the display name recomposed from the
    // translated template + line matches the English SeNa composed by the feed byte-for-byte.
    // Over/Under templates append " {line}"; other handicap markets append the signed line, with
    // the sign flipped for SignFlipHandicapTemplateIds. Non-handicap markets are unchanged.
    // Template-id numbering is bet-genius's SeTeId scheme (the only source in production today) --
    // verified against bet-genius/BG.Receiver.Service/Repositories/OddsFeedRepository.cs and live
    // Mongo Selection documents (SeTeId "30"/"31" -> SeNa "Over"/"Under").
    public static class SelectionNameComposer
    {
        private static readonly List<string> OverUnderTemplateIds = new List<string> { "30", "31" };
        private static readonly List<string> SignFlipHandicapTemplateIds = new List<string> { "1", "4" };

        public static string Compose(string line, string selectionName, string selectionTemplateId, bool isHandicap)
        {
            if (!isHandicap)
                return selectionName;

            var isOverUnder = OverUnderTemplateIds.Contains(selectionTemplateId);

            if (isOverUnder)
                return $"{selectionName} {line}";

            decimal decLine;
            try
            {
                decLine = Convert.ToDecimal(line);
            }
            catch
            {
                return selectionName; // malformed line — safest to leave the template name untouched
            }

            var absLine = Math.Abs(decLine);
            var isSignFlip = SignFlipHandicapTemplateIds.Contains(selectionTemplateId);

            if (decLine == 0m)
                return $"{selectionName}(0)";

            if (decLine < 0)
                return isSignFlip
                    ? $"{selectionName}(+{absLine})"
                    : $"{selectionName}(-{absLine})";

            return isSignFlip
                ? $"{selectionName}(-{absLine})"
                : $"{selectionName}(+{absLine})";
        }
    }
}

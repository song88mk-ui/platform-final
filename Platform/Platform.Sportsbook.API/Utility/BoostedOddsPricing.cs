using Platform.Shared.Data.Sportsbook.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Platform.Sportsbook.API.Utility
{
    // The boosted price of a selection for a client, as odds-engine-2 stamps it on the selection document:
    // Selection.ClBoOd is "CODE:odds|CODE:odds", one entry per boosting client (OE.Sportsbook.Service
    // PriceBoostMerger writes it, BO-10). The Price Boost tab and the bet slip read it through here.
    //
    // ORIGINAL ODDS vs CLIENT ODDS: a boost replaces the ORIGINAL odds of the selection (Selection.Od as the
    // provider sends it). That holds while SportsbookRepository.IsOEEnable is false, which is today: clients
    // are priced with the original odds and no margin is applied. The day clients are priced with CLIENT
    // odds (IsOEEnable true, margin from the market's ClCoMa), the boosted price has to be defined again
    // against them: the boosted odds Backoffice sets, the cap OE.Job.BoostedOddsCap compares them with, and
    // the stale-odds check of a boosted bet and the price shown to Genius Sports (the original odds,
    // GTSRepository.GetProviderPrice) were all built on the original odds. The tripwire in
    // BoostedOddsPricingTests fails the day IsOEEnable can return true, so that this is looked at then.
    public static class BoostedOddsPricing
    {
        private const char ENTRY_SEPARATOR = '|';
        private const char KEY_SEPARATOR = ':';

        // The client's boosted odds on the selection. False when the selection has no boost for the client, or
        // when its entry is not a positive price. The client is matched on its whole code: G0 is not G01.
        public static bool TryGetClientBoostedOdds(string clientBoostedOdds, string clientCode, out decimal boostedOdds)
        {
            boostedOdds = 0m;

            if (string.IsNullOrWhiteSpace(clientBoostedOdds) || string.IsNullOrWhiteSpace(clientCode))
                return false;

            foreach (var entry in clientBoostedOdds.Split(ENTRY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = entry.IndexOf(KEY_SEPARATOR);

                if (separator <= 0 || !string.Equals(entry.Substring(0, separator), clientCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (decimal.TryParse(entry.Substring(separator + 1), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0m)
                {
                    boostedOdds = parsed;
                    return true;
                }

                return false;
            }

            return false;
        }

        // The odds to show for a selection, as a "0.00" string: the client's boosted odds when the request is a
        // boosted one and the client has a boost on the selection, otherwise the odds it was given.
        public static string GetClientBoostedOdds(string clientBoostedOdds, string clientCode, bool isBoosted, string odds)
        {
            if (!isBoosted)
                return odds;

            return TryGetClientBoostedOdds(clientBoostedOdds, clientCode, out var boostedOdds)
                ? boostedOdds.ToString("0.00", CultureInfo.InvariantCulture)
                : odds;
        }

        // Prices the selections of a boosted bet at the client's boosted odds. OddsRef keeps the original odds.
        // A selection the client has no boost on keeps its odds, so the stale-odds check refuses a bet placed
        // on a boost that has since been removed.
        public static void Apply(IEnumerable<SelectionExtension> selections, string clientCode)
        {
            foreach (var selection in selections)
            {
                if (TryGetClientBoostedOdds(selection.ClBoOd, clientCode, out var boostedOdds))
                    selection.Od = boostedOdds;
            }
        }
    }
}

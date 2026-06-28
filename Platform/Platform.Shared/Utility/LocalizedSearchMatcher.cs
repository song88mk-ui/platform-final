using System;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Shared.Utility
{
    public enum SearchMatchKind
    {
        None,
        StartsWith,
        Contains
    }

    /// <summary>
    /// Pure, dependency-free matching rules for localized (non-EN) sportsbook search.
    /// The repository resolves the localized name from the SQL translation store (falling back to
    /// the base English name) and feeds it here, so search matches exactly what the user sees.
    /// Extracted from SportsbookRepository.GetSearchResults so the rules can be unit tested without
    /// MongoDB / SQL dependencies. A prefix match ranks above a mid-string ("contains") match.
    /// </summary>
    public static class LocalizedSearchMatcher
    {
        /// <summary>League / segment names are matched as a whole string.</summary>
        public static SearchMatchKind MatchLeague(string localizedLeagueName, string searchText)
        {
            if (string.IsNullOrEmpty(localizedLeagueName) || string.IsNullOrEmpty(searchText))
                return SearchMatchKind.None;

            var name = localizedLeagueName.ToLower();
            var term = searchText.ToLower();

            if (name.StartsWith(term))
                return SearchMatchKind.StartsWith;
            if (name.Contains(term))
                return SearchMatchKind.Contains;
            return SearchMatchKind.None;
        }

        /// <summary>
        /// Fixture names are "Home vs. Away". EN uses " vs. " but localized names may omit the
        /// surrounding spaces (e.g. "保加利亚vs.乌克兰"), so split on the bare "vs." and trim.
        /// A prefix match on either participant ranks above a mid-string match on the full name.
        /// </summary>
        public static SearchMatchKind MatchFixture(string localizedFixtureName, string searchText)
        {
            if (string.IsNullOrEmpty(localizedFixtureName) || string.IsNullOrEmpty(searchText))
                return SearchMatchKind.None;

            var name = localizedFixtureName.ToLower();
            var term = searchText.ToLower();

            var participants = name.Split(new[] { "vs." }, StringSplitOptions.None)
                                   .Select(p => p.Trim())
                                   .ToList();
            var home = participants.FirstOrDefault() ?? string.Empty;
            var away = participants.Count > 1 ? participants.Last() : string.Empty;

            if (home.StartsWith(term) || away.StartsWith(term))
                return SearchMatchKind.StartsWith;
            if (name.Contains(term))
                return SearchMatchKind.Contains;
            return SearchMatchKind.None;
        }
    }
}

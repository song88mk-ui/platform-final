using Platform.Shared.MessageModels;
using Platform.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace Platform.Shared.Helpers
{
    // Maps the Translations list of a v1/cachepreload request (sent by the translation-db-updater
    // script) to the OnTranslationChange message Platform.API publishes to Platform.CacheSync.Service.
    // Lives here rather than in Platform.API so it can be tested: Platform.API has no test project.
    public static class TranslationChangeMapper
    {
        // Rows without a StringId or TargetLanguage are dropped, since no consumer can apply them.
        // A null TargetText becomes an empty Tx, which consumers treat as a removal. SourceText is
        // passed through as SrcTx (GM-852): OE.Translation.Service uses it to scope a MaTe update to
        // the markets whose MaNa carries that wording.
        public static OnTranslationChangeMessage ToMessage(IEnumerable<TranslationChangeRequestDTO> translations)
        {
            return new OnTranslationChangeMessage
            {
                Tr = (translations ?? Enumerable.Empty<TranslationChangeRequestDTO>())
                    .Where(t => t != null
                        && !string.IsNullOrWhiteSpace(t.StringId)
                        && !string.IsNullOrWhiteSpace(t.TargetLanguage))
                    .Select(t => new TranslationChangeItem
                    {
                        StId = t.StringId,
                        LaCo = t.TargetLanguage,
                        Tx = t.TargetText ?? string.Empty,
                        SrcTx = t.SourceText
                    })
                    .ToList()
            };
        }
    }
}

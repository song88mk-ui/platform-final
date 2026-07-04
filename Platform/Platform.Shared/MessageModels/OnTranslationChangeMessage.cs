using System.Collections.Generic;

namespace Platform.Shared.MessageModels
{
    // Payload-carrying successor to the purge-only OnDynamicTranslationChange flow. Wire-compatible
    // with OE.Shared's OnTranslationChangeMessage (odds-engine-2). Flow: external translation script
    // updates TranslationCache2, then calls Platform.API POST v1/cachepreload with the changed rows;
    // Platform.API publishes this to Platform.CacheSync.Service (r.oe.cachesync.ontranslationchange),
    // which purges the Sportsbook.API translation cache and forwards it to OE.Adapter.Service
    // (r.oe.ontranslationchange).
    public class OnTranslationChangeMessage
    {
        public List<TranslationChangeItem> Tr { get; set; } = new List<TranslationChangeItem>();
    }

    public class TranslationChangeItem
    {
        // TranslationCache2.STRING_ID — glossary key formats:
        // "SpId:{id}" | "SegId:{id}" | "LeId:{id}" | "Te:{teamId}" | "MaTe:{MaTeId}" | "SeTe:{MaTeId}:{SeTeId}"
        public string StId { get; set; }
        public string LaCo { get; set; } // target language code, e.g. "ZH" | "ES" | "FR"
        public string Tx { get; set; }   // translated text; empty string clears the translation
    }
}

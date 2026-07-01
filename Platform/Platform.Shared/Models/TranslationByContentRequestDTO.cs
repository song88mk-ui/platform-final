using System.Collections.Generic;
using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class TranslationByContentRequestDTO
    {
        public Dictionary<string, string> KeyToSourceText { get; set; }
        public LanguageCode LanguageCode { get; set; }
    }
}

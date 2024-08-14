using Platform.Shared.Enums;

namespace Platform.Shared.Models
{
    public class TranslationRecoveryRequestDTO
    {
        public string LanguageCode { get; set; }
        public string Specifier { get; set; }
    }
}

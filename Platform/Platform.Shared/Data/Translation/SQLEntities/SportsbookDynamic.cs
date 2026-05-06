using System.ComponentModel.DataAnnotations;

namespace Platform.Shared.Data.Translation.SQLEntities
{
    public class SportsbookDynamic
    {
        [Key]
        [Required]
        [MaxLength(100)]
        public string String_ID { get; set; }

        [Required]
        public string VALUE_EN { get; set; }

        public string VALUE_ZH { get; set; }
        public string VALUE_ID { get; set; }
        public string VALUE_BR { get; set; }
        public string VALUE_DE { get; set; }
        public string VALUE_ES { get; set; }
        public string VALUE_FI { get; set; }
        public string VALUE_FR { get; set; }
        public string VALUE_JA { get; set; }
        public string VALUE_KO { get; set; }
        public string VALUE_NL { get; set; }
        public string VALUE_PT { get; set; }
        public string VALUE_TH { get; set; }
        public string VALUE_TR { get; set; }
        public string VALUE_VI { get; set; }
    }
}

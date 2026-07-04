using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Translation.SQLEntities
{
    // Non-English translations, written by the external translation script and keyed by
    // SOURCE_HASH (= CONVERT(varchar(64), HASHBYTES('SHA2_256', SOURCE_TEXT), 2)) -- the PK, together
    // with SOURCE_LANGUAGE/TARGET_LANGUAGE. Platform.Sportsbook.API resolves the default content-hash
    // path by SOURCE_HASH; the opt-in template-key path (Custom:Translation:UseTemplateKey) resolves by
    // STRING_ID instead -- the same SportsbookGlossary key bet-genius writes (e.g. "MaTe:1234",
    // "SeTe:1234:56", "Te:789"). STRING_ID is required and defaults to SOURCE_HASH for rows that
    // predate it / have no mapped glossary key.
    public class TranslationCache
    {
        [Required]
        [MaxLength(64)]
        [Column("SOURCE_HASH")]
        public string SourceHash { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("STRING_ID")]
        public string StringId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("SOURCE_LANGUAGE")]
        public string SourceLanguage { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("TARGET_LANGUAGE")]
        public string TargetLanguage { get; set; }

        [Column("SOURCE_TEXT")]
        public string SourceText { get; set; }

        [Column("TARGET_TEXT")]
        public string? TargetText { get; set; }

        [Column("CREATED_AT")]
        public DateTime? CreatedAt { get; set; }

        [Column("GLOSSARY_VERSION")]
        public int? GlossaryVersion { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }
    }
}

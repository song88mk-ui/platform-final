using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Data.Translation.SQLEntities
{
    public class SportsbookDynamic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("String_ID")]
        public string StringId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("SOURCE_LANGUAGE_ID")]
        public string SourceLanguageId { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("TARGET_LANGUAGE_ID")]
        public string TargetLanguageId { get; set; }

        [Required]
        [Column("SOURCE_TEXT")]
        public string SourceText { get; set; }

        [Column("TARGET_TEXT")]
        public string? TargetText { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("SOURCE_HASH")]
        public string SourceHash { get; private set; }

        [Required]
        [Column("GLOSSARY_VERSION")]
        public int GlossaryVersion { get; set; }

        [Required]
        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; }

        [Column("UPDATED_AT")]
        public DateTime? UpdatedAt { get; set; }

        [Required]
        [Column("PROCESSED")]
        public bool Processed { get; set; }
    }
}

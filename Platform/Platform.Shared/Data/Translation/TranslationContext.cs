using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Translation.SQLEntities;

namespace Platform.Shared.Data.Sportsbook
{
    public sealed class TranslationContext : DbContext
    {
        public TranslationContext(DbContextOptions<TranslationContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SportsbookDynamic>(entity =>
            {
                // Table
                entity.ToTable("SportsbookDynamic");

                // Primary Key
                entity.HasKey(e => e.Id)
                    .HasName("PK_SportsbookDynamic_v2");

                // Unique Index
                entity.HasIndex(e => new
                {
                    e.StringId,
                    e.TargetLanguageId
                })
                .IsUnique()
                .HasName("UX_SBD_STRING_LANG");

                // Non-Unique Lookup Index
                entity.HasIndex(e => new
                {
                    e.StringId,
                    e.TargetLanguageId
                })
                .HasName("IX_SBD_STRING_LANG");

                // Hash + Language + Version Index
                entity.HasIndex(e => new
                {
                    e.SourceHash,
                    e.SourceLanguageId,
                    e.TargetLanguageId,
                    e.GlossaryVersion
                })
                .HasName("IX_SBD_HASH_LANG");

                // Missing Translation Filtered Index
                entity.HasIndex(e => e.TargetLanguageId)
                    .HasName("IX_SBD_MISSING")
                    .HasFilter("[TARGET_TEXT] IS NULL");

                // Property Configurations

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.StringId)
                    .HasColumnName("String_ID")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.SourceLanguageId)
                    .HasColumnName("SOURCE_LANGUAGE_ID")
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(e => e.TargetLanguageId)
                    .HasColumnName("TARGET_LANGUAGE_ID")
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(e => e.SourceText)
                    .HasColumnName("SOURCE_TEXT")
                    .IsRequired();

                entity.Property(e => e.TargetText)
                    .HasColumnName("TARGET_TEXT");

                entity.Property(e => e.SourceHash)
                    .HasColumnName("SOURCE_HASH")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.GlossaryVersion)
                    .HasColumnName("GLOSSARY_VERSION")
                    .HasDefaultValue(1);

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CREATED_AT")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UPDATED_AT");

                entity.Property(e => e.Processed)
                    .HasColumnName("PROCESSED")
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<SportsbookStatic>().HasKey(k => new { k.LanguageCode, k.Name });

            // Non-English translations written by the external translation script, keyed by SOURCE_HASH.
            modelBuilder.Entity<TranslationCache>(entity =>
            {
                entity.ToTable("TranslationCache");

                entity.HasKey(e => new { e.SourceHash, e.SourceLanguage, e.TargetLanguage })
                    .HasName("PK_TranslationCache");

                entity.Property(e => e.SourceHash).HasColumnName("SOURCE_HASH").HasMaxLength(64).IsRequired();
                entity.Property(e => e.SourceLanguage).HasColumnName("SOURCE_LANGUAGE").HasMaxLength(10).IsRequired();
                entity.Property(e => e.TargetLanguage).HasColumnName("TARGET_LANGUAGE").HasMaxLength(10).IsRequired();
                entity.Property(e => e.SourceText).HasColumnName("SOURCE_TEXT");
                entity.Property(e => e.TargetText).HasColumnName("TARGET_TEXT");
                entity.Property(e => e.CreatedAt).HasColumnName("CREATED_AT");
                entity.Property(e => e.GlossaryVersion).HasColumnName("GLOSSARY_VERSION");
                entity.Property(e => e.UpdatedAt).HasColumnName("UPDATED_AT");
            });
        }
        public DbSet<SportsbookDynamic> SportsbookDynamic { get; set; }
        public DbSet<SportsbookStatic> SportsbookStatic { get; set; }
        public DbSet<TranslationCache> TranslationCache { get; set; }

    }
}

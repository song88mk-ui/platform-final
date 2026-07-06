using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Translation.SQLEntities;

namespace Platform.Shared.Data.Sportsbook
{
    // Static UI-label store only. The glossary-era SportsbookDynamic / TranslationCache
    // entities were removed with the 2026-07 translation-flow redesign: dynamic names are
    // resolved from the Mongo *NaTr lists, sourced from the flat TranslationCache2 table
    // owned by odds-engine-2 (OE.Shared\Data\Translation) and the external translation
    // pipeline — the Platform reads no dynamic-translation SQL.
    public sealed class TranslationContext : DbContext
    {
        public TranslationContext(DbContextOptions<TranslationContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SportsbookStatic>().HasKey(k => new { k.LanguageCode, k.Name });
        }
        public DbSet<SportsbookStatic> SportsbookStatic { get; set; }
    }
}

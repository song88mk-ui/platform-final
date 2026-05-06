using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Translation.SQLEntities;

namespace Platform.Shared.Data.Sportsbook
{
    public sealed class TranslationContext : DbContext
    {
        public TranslationContext(DbContextOptions<TranslationContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SportsbookDynamic>().HasKey(k => new { k.String_ID });
            modelBuilder.Entity<SportsbookStatic>().HasKey(k => new { k.LanguageCode, k.Name });
        }
        public DbSet<SportsbookDynamic> SportsbookDynamic { get; set; }
        public DbSet<SportsbookStatic> SportsbookStatic { get; set; }

    }
}

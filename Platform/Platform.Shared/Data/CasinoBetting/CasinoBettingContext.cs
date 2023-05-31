using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.CasinoBetting.Entities;

namespace Platform.Shared.Data.CasinoBetting
{
    public class CasinoBettingContext : DbContext
    {
        public CasinoBettingContext(DbContextOptions<CasinoBettingContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CasinoBet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<CasinoBetAudit>().HasKey(k => new { k.Id });
        }

        public DbSet<CasinoBet> Transaction { get; set; }
        public DbSet<CasinoBetAudit> TransactionAudit { get; set; }
    }
}

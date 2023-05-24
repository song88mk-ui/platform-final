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
            modelBuilder.Entity<Transaction>().HasKey(k => new { k.TransactionId });
            modelBuilder.Entity<TransactionAudit>().HasKey(k => new { k.Id });
        }

        public DbSet<Transaction> Transaction { get; set; }
        public DbSet<TransactionAudit> TransactionAudit { get; set; }
    }
}

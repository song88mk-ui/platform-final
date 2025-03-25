using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.RedCatBetting.Entities;

namespace Platform.Shared.Data.RedCatBetting
{
    public class RedCatBettingContext : DbContext
    {
        public RedCatBettingContext(DbContextOptions<RedCatBettingContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<BetAudit>().HasKey(k => new { k.TransactionId, k.Id });
            modelBuilder.Entity<BetSelection>().HasKey(k => new { k.BetSelectionId });
            modelBuilder.Entity<BetSelectionAudit>().HasKey(k => new { k.Id });
            //modelBuilder.Entity<BetMapping>().HasKey(k => new { k.BetIdRef });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
        //public DbSet<BetMapping> BetMapping { get; set; }
    }
}

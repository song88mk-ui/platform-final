using Microsoft.EntityFrameworkCore;
using Platform.Operator.Service.Entities;

namespace Platform.Operator.Service.Data
{
    public class BettingContext : DbContext
    {
        public BettingContext(DbContextOptions<BettingContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<BetAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelection>().HasKey(k => new { k.BetSelectionId });
            modelBuilder.Entity<BetSelectionAudit>().HasKey(k => new { k.Id });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;

namespace Platform.Job.BetAudit.Tableau.Data.Betting
{
    public class BettingContextSA : DbContext
    {
        public BettingContextSA(DbContextOptions<BettingContextSA> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BetAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAudit>().HasKey(k => new { k.Id });
        }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
    }
}

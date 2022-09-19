using Microsoft.EntityFrameworkCore;

namespace Platform.Job.BetAudit.Tableau.Data.Betting
{
    public class BettingContextHK : DbContext
    {
        public BettingContextHK(DbContextOptions<BettingContextHK> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BetAudit_bak2>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAudit_bak2>().HasKey(k => new { k.Id });

            modelBuilder.Entity<BetAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAudit>().HasKey(k => new { k.Id });

            modelBuilder.Entity<Bet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<BetSelection>().HasKey(k => new { k.BetSelectionId, k.BetId });
        }
        public DbSet<BetAudit_bak2> BetAudit_bak2 { get; set; }
        public DbSet<BetSelectionAudit_bak2> BetSelectionAudit_bak2 { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
    }
}

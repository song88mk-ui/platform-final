using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Reports.Entities;

namespace Platform.Shared.Data.Reports
{
    public class AgentReportsContext : DbContext
    {
        public AgentReportsContext(DbContextOptions<AgentReportsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bet>().HasKey(k => new { k.ParentUserId, k.BetId });
            modelBuilder.Entity<BetSelection>().HasKey(k => new { k.ParentUserId, k.BetId, k.BetSelectionId });
            modelBuilder.Entity<BetSelectionTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<WinLoss>().HasKey(k => new { k.ParentUserId, k.DirectUserId, k.IndirectUserId, k.TransactionDate, k.BetId });

            modelBuilder.Entity<BetTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<RunningBalance>().HasKey(k => new { k.ParentUserId, k.UserId });
            modelBuilder.Entity<RunningBalanceTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<CashBalanceTracker>().HasKey(k => new { k.ParentUserId, k.UserId, k.BetId });
            modelBuilder.Entity<PartnerRevenueShare>().HasKey(k => new { k.Id }); ;
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetTransaction> BetTransaction { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionTransaction> BetSelectionTransaction { get; set; }
        public DbSet<WinLoss> WinLoss { get; set; }
        public DbSet<RunningBalance> RunningBalance { get; set; }
        public DbSet<RunningBalanceTransaction> RunningBalanceTransaction { get; set; }
        public DbSet<CashBalanceTracker> CashBalanceTracker { get; set; }
        public DbSet<PartnerRevenueShare> PartnerRevenueShare { get; set; }
    }
}

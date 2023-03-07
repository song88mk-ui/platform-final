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
            modelBuilder.Entity<BetFinal>().HasKey(k => new { k.ParentUserId, k.BetId });
            modelBuilder.Entity<BetSelectionFinal>().HasKey(k => new { k.ParentUserId, k.BetId, k.BetSelectionId });
            modelBuilder.Entity<BetSelectionTransactionFinal>().HasKey(k => new { k.Id });
            modelBuilder.Entity<WinLossFinal>().HasKey(k => new { k.ParentUserId, k.DirectUserId, k.IndirectUserId, k.TransactionDate, k.BetId });

            modelBuilder.Entity<BetTransactionFinal>().HasKey(k => new { k.Id });
            modelBuilder.Entity<RunningBalanceFinal>().HasKey(k => new { k.ParentUserId, k.UserId });
            modelBuilder.Entity<RunningBalanceTransactionFinal>().HasKey(k => new { k.Id });
            modelBuilder.Entity<CashBalanceTrackerFinal>().HasKey(k => new { k.ParentUserId, k.UserId, k.BetId });
            modelBuilder.Entity<PartnerRevenueShareFinal>().HasKey(k => new { k.Id }); ;
        }

        public DbSet<BetFinal> BetFinal { get; set; }
        public DbSet<BetTransactionFinal> BetTransaction { get; set; }
        public DbSet<BetSelectionFinal> BetSelectionFinal { get; set; }
        public DbSet<BetSelectionTransactionFinal> BetSelectionTransactionFinal { get; set; }
        public DbSet<WinLossFinal> WinLossFinal { get; set; }
        public DbSet<RunningBalanceFinal> RunningBalance { get; set; }
        public DbSet<RunningBalanceTransactionFinal> RunningBalanceTransaction { get; set; }
        public DbSet<CashBalanceTrackerFinal> CashBalanceTracker { get; set; }
        public DbSet<PartnerRevenueShareFinal> PartnerRevenueShare { get; set; }
    }
}

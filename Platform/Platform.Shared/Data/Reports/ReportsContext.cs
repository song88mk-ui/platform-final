using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Reports.Entities;

namespace Platform.Shared.Data.Reports
{
    public class ReportsContext : DbContext
    {
        public ReportsContext(DbContextOptions<ReportsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BetFinal>().HasKey(k => new { k.ParentUserId, k.BetId });
            modelBuilder.Entity<BetSelectionFinal>().HasKey(k => new { k.ParentUserId, k.BetId, k.BetSelectionId });
            modelBuilder.Entity<BetSelectionTransactionFinal>().HasKey(k => new { k.Id });
            modelBuilder.Entity<WinLossFinal>().HasKey(k => new { k.ParentUserId, k.DirectUserId, k.IndirectUserId, k.TransactionDate, k.BetId });

            modelBuilder.Entity<BetTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<RunningBalance>().HasKey(k => new { k.ParentUserId, k.UserId });
            modelBuilder.Entity<RunningBalanceTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<CashBalanceTracker>().HasKey(k => new { k.ParentUserId, k.UserId, k.BetId });
            modelBuilder.Entity<PartnerRevenueShare>().HasKey(k => new { k.Id }); ;
        }

        public DbSet<BetFinal> BetFinal { get; set; }
        public DbSet<BetTransaction> BetTransaction { get; set; }
        public DbSet<BetSelectionFinal> BetSelectionFinal { get; set; }
        public DbSet<BetSelectionTransactionFinal> BetSelectionTransactionFinal { get; set; }
        public DbSet<WinLossFinal> WinLossFinal { get; set; }
        public DbSet<RunningBalance> RunningBalance { get; set; }
        public DbSet<RunningBalanceTransaction> RunningBalanceTransaction { get; set; }
        public DbSet<CashBalanceTracker> CashBalanceTracker { get; set; }
        public DbSet<PartnerRevenueShare> PartnerRevenueShare { get; set; }
    }
}

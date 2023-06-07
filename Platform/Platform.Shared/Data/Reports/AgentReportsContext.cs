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
            modelBuilder.Entity<CasinoWinLoss>().HasKey(k => new { k.ParentUserId, k.DirectUserId, k.IndirectUserId, k.BetDate, k.BetId });
            modelBuilder.Entity<ProductLookup>().HasKey(k => new { k.ProductTypeId, k.GameType, k.ProviderName, k.ProductName });
            modelBuilder.Entity<WinLossSummary>().HasNoKey();
            modelBuilder.Entity<WinLossDetails>().HasNoKey();
            modelBuilder.Entity<ProfitLossSummary>().HasNoKey();
            modelBuilder.Entity<ProfitLossDetails>().HasNoKey();
            modelBuilder.Entity<GGRSummary>().HasNoKey();
            modelBuilder.Entity<GGRDetails>().HasNoKey();

            modelBuilder.Entity<BetTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<RunningBalance>().HasKey(k => new { k.ParentUserId, k.UserId });
            modelBuilder.Entity<RunningBalanceTransaction>().HasKey(k => new { k.Id });
            modelBuilder.Entity<CashBalanceTracker>().HasKey(k => new { k.ParentUserId, k.UserId, k.BetId });
            modelBuilder.Entity<PartnerRevenueShare>().HasKey(k => new { k.Id }); ;
            modelBuilder.Entity<TransferHistory>().HasKey(k => new { k.ParentUserId, k.UserId, k.ReferenceId });
            modelBuilder.Entity<SettlementHistory>().HasKey(k => new { k.Id });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetTransaction> BetTransaction { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionTransaction> BetSelectionTransaction { get; set; }
        public DbSet<WinLoss> WinLoss { get; set; }
        public DbSet<CasinoWinLoss> CasinoWinLoss { get; set; }
        public DbSet<RunningBalance> RunningBalance { get; set; }
        public DbSet<RunningBalanceTransaction> RunningBalanceTransaction { get; set; }
        public DbSet<CashBalanceTracker> CashBalanceTracker { get; set; }
        public DbSet<PartnerRevenueShare> PartnerRevenueShare { get; set; }
        public DbSet<TransferHistory> TransferHistory { get; set; }
        public DbSet<SettlementHistory> SettlementHistory { get; set; }
        public DbSet<ProductLookup> ProductLookup { get; set; }

        // Stored Procedure
        public DbSet<WinLossSummary> ReadWinLossSummary { get; set; }
        public DbSet<WinLossDetails> ReadWinLossDetails { get; set; }
        public DbSet<ProfitLossSummary> ReadProfitLossSummary { get; set; }
        public DbSet<ProfitLossDetails> ReadProfitLossDetails { get; set; }
        public DbSet<GGRSummary> ReadGGRSummary { get; set; }
        public DbSet<GGRDetails> ReadGGRDetails { get; set; }
    }
}

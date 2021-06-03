using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting.Entities;

namespace Platform.Shared.Data.Betting
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
            modelBuilder.Entity<BetConfig>().HasKey(k => new { k.ClientId, k.CurrencyCode });
            modelBuilder.Entity<StakeLimitFixture>().HasKey(k => new { k.UserId, k.FixtureId });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
        public DbSet<BetConfig> BetConfig { get; set; }
        public DbSet<StakeLimitFixture> StakeLimitFixture { get; set; }
    }
}

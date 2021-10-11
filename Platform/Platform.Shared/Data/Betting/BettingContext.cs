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
            modelBuilder.Entity<ParlayBonus>().HasKey(k => new { k.ClientIdRef, k.NoOfSelection });
            modelBuilder.Entity<ParlayBonusAudit>().HasKey(k => new { k.Id });
            modelBuilder.Entity<StakeLimitFixture>().HasKey(k => new { k.UserId, k.FixtureId });
            modelBuilder.Entity<Tier>().HasKey(k => new { k.TierId });
            modelBuilder.Entity<League>().HasKey(k => new { k.Id });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
        public DbSet<BetSelection> BetSelection { get; set; }
        public DbSet<BetSelectionAudit> BetSelectionAudit { get; set; }
        public DbSet<BetConfig> BetConfig { get; set; }
        public DbSet<ParlayBonus> ParlayBonus { get; set; }
        public DbSet<ParlayBonusAudit> ParlayBonusAudit { get; set; }
        public DbSet<StakeLimitFixture> StakeLimitFixture { get; set; }
        public DbSet<League> League { get; set; }
        public DbSet<Tier> Tier { get; set; }
    }
}

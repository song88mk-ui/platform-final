using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Sportsbook.SQLEntities;

namespace Platform.Shared.Data.Sportsbook
{
    public class SportsbookContext : DbContext
    {
        public SportsbookContext(DbContextOptions<SportsbookContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientSportOrder>().HasKey(k => new { k.ClientId, k.SportId });
            modelBuilder.Entity<ClientTopFixture>().HasKey(k => new { k.ClientId, k.FixtureId });
            modelBuilder.Entity<ClientLeagueOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<TierLeagueOrder>().HasKey(k => new { k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<MarketTemplateGroup>().HasKey(k => new { k.SportId, k.FixtureStatusId, k.Id });
            modelBuilder.Entity<UserFavorite>().HasKey(k => new { k.UserId, k.Id });
        }

        public DbSet<ClientSportOrder> ClientSportOrder { get; set; }
        public DbSet<ClientTopFixture> ClientTopFixture { get; set; }
        public DbSet<ClientLeagueOrder> ClientLeagueOrder { get; set; }
        public DbSet<TierLeagueOrder> TierLeagueOrder { get; set; }
        public DbSet<MarketTemplateGroup> MarketTemplateGroup { get; set; }
        public DbSet<UserFavorite> UserFavorite { get; set; }
    }
}

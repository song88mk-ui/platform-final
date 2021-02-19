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
            modelBuilder.Entity<TierLeagueOrder>().HasKey(k => new { k.SportId, k.SegmentId, k.LeagueId });
        }

        public DbSet<ClientSportOrder> ClientSportOrder { get; set; }
        public DbSet<TierLeagueOrder> TierLeagueOrder { get; set; }
    }
}

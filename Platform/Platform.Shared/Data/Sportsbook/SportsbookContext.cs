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
            modelBuilder.Entity<ClientTopLeague>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<TierLeagueOrder>().HasKey(k => new { k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<MarketTemplateGroup>().HasKey(k => new { k.SportId, k.FixtureStatusId, k.Id });
            modelBuilder.Entity<UserFavorite>().HasKey(k => new { k.UserId, k.SportId, k.Id });
            modelBuilder.Entity<SportGroup>().HasKey(k => new { k.SportGroupId });
            modelBuilder.Entity<SelectorMarketTemplate>().HasKey(k => new { k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<ClientSegmentOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId });
            modelBuilder.Entity<ClientLeagueOrder>().HasKey(k => new { k.ClientId, k.SportId, k.SegmentId, k.LeagueId });
            modelBuilder.Entity<ClientMarketTemplateOrder>().HasKey(k => new { k.ClientId, k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<ClientMarketTemplateGroup>().HasKey(k => new { k.ClientId, k.SportId, k.FixtureStatusId, k.Id });
            modelBuilder.Entity<BetBuilderMarketTemplates>().HasKey(k => new { k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<MarketTemplatePlayerProps>().HasKey(k => new { k.SportId, k.FixtureStatusId });
            modelBuilder.Entity<ClientSelectorMarketTemplate>().HasKey(k => new { k.ClientId, k.SportId, k.MarketTemplateId });
            modelBuilder.Entity<MarketTemplatePlayerProps>().HasKey(k => new { k.SportId, k.FixtureStatusId });
            modelBuilder.Entity<Translation>().HasKey(k => new { k.LanguageCode, k.Name });
            modelBuilder.Entity<Country>().HasKey(k => new { k.Id });
            modelBuilder.Entity<Announcement>().HasKey(k => new { k.Id });
            modelBuilder.Entity<ClientAnnouncements>().HasKey(k => new { k.ClientId, k.AnnouncementId });
            modelBuilder.Entity<LanguageAnnouncements>().HasKey(k => new { k.LangCode, k.AnnouncementId });
        }

        public DbSet<ClientSportOrder> ClientSportOrder { get; set; }
        public DbSet<ClientTopFixture> ClientTopFixture { get; set; }
        public DbSet<ClientTopLeague> ClientTopLeague { get; set; }
        public DbSet<TierLeagueOrder> TierLeagueOrder { get; set; }
        public DbSet<MarketTemplateGroup> MarketTemplateGroup { get; set; }
        public DbSet<UserFavorite> UserFavorite { get; set; }
        public DbSet<SportGroup> SportGroup { get; set; }
        public DbSet<SelectorMarketTemplate> SelectorMarketTemplate { get; set; }
        public DbSet<ClientSegmentOrder> ClientSegmentOrder { get; set; }
        public DbSet<ClientLeagueOrder> ClientLeagueOrder { get; set; }
        public DbSet<ClientMarketTemplateOrder> ClientMarketTemplateOrder { get; set; }
        public DbSet<ClientMarketTemplateGroup> ClientMarketTemplateGroup { get; set; }
        public DbSet<BetBuilderMarketTemplates> BetBuilderMarketTemplates { get; set; }
        public DbSet<MarketTemplatePlayerProps> MarketTemplatePlayerProps { get; set; }
        public DbSet<ClientSelectorMarketTemplate> ClientSelectorMarketTemplate { get; set; }
        public DbSet<Translation> Translation { get; set; }
        public DbSet<Country> Country { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<ClientAnnouncements> ClientAnnouncements { get; set; }
        public DbSet<LanguageAnnouncements> LanguageAnnouncements { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.Betting.Entities;
using Platform.Shared.Data.Platform.Entities;

namespace Platform.Shared.Data.Platform
{
    public class PlatformContext : DbContext
    {
        public PlatformContext(DbContextOptions<PlatformContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>().HasKey(k => new { k.ClientId });
            modelBuilder.Entity<Operator>().HasKey(k => new { k.UserId });
            modelBuilder.Entity<ForexEUR>().HasKey(k => new { k.CurrencyCode });
            modelBuilder.Entity<OperatorLog>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetConfig>().HasKey(k => new { k.ClientId, k.SportsId, k.CurrencyCode });
        }

        public DbSet<Client> Client { get; set; }
        public DbSet<Operator> Operator { get; set; }
        public DbSet<ForexEUR> ForexEUR { get; set; }
        public DbSet<OperatorLog> OperatorLog { get; set; }
        public DbSet<BetConfig> BetConfig { get; set; }
    }
}

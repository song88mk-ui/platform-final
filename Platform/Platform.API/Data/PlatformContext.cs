using Microsoft.EntityFrameworkCore;
using Platform.API.Data.Entities;

namespace Platform.API.Data
{
    public class PlatformContext : DbContext
    {
        public PlatformContext(DbContextOptions<PlatformContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>().HasKey(k => new { k.ClientId });
            modelBuilder.Entity<ForexEUR>().HasKey(k => new { k.CurrencyCode });
        }

        public DbSet<Client> Client { get; set; }
        public DbSet<ForexEUR> ForexEUR { get; set; }
    }
}

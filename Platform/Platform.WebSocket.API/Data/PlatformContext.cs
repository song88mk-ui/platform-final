using Microsoft.EntityFrameworkCore;

namespace Platform.WebSocket.API.Data
{
    public class PlatformContext : DbContext
    {
        public PlatformContext(DbContextOptions<PlatformContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>().HasKey(k => new { k.ClientId });
            modelBuilder.Entity<WebSocket>().HasKey(k => new { k.ConnectionId });
        }

        public DbSet<Client> Client { get; set; }
        public DbSet<WebSocket> WebSocket { get; set; }
    }
}

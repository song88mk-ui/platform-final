using Microsoft.EntityFrameworkCore;

namespace Platform.Operator.API.Data
{
    public class PlatformContext : DbContext
    {
        public PlatformContext(DbContextOptions<PlatformContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entities.Operator>().HasKey(k => new { k.UserId });
        }

        public DbSet<Entities.Operator> Operator { get; set; }
    }
}

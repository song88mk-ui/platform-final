using Microsoft.EntityFrameworkCore;
using Platform.Operator.Service.Data.Entities;

namespace Platform.API.Data
{
    public class PlatformContext : DbContext
    {
        public PlatformContext(DbContextOptions<PlatformContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OperatorLog>().HasKey(k => new { k.Id });
        }

        public DbSet<OperatorLog> OperatorLog { get; set; }
    }
}

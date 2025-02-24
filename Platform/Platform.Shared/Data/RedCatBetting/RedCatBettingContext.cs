using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.RedCatBetting.Entities;

namespace Platform.Shared.Data.RedCatBetting
{
    public class RedCatBettingContext : DbContext
    {
        public RedCatBettingContext(DbContextOptions<RedCatBettingContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<BetAudit>().HasKey(k => new { k.Id });
        }

        public DbSet<Bet> Bet { get; set; }
        public DbSet<BetAudit> BetAudit { get; set; }
    }
}

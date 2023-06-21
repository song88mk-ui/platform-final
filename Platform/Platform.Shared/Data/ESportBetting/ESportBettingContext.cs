using Microsoft.EntityFrameworkCore;
using Platform.Shared.Data.ESportBetting.SQLEntities;

namespace Platform.Shared.Data.ESportBetting
{
    public class ESportBettingContext : DbContext
    {
        public ESportBettingContext(DbContextOptions<ESportBettingContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ESportBet>().HasKey(k => new { k.BetId });
            modelBuilder.Entity<ESportBetAudit>().HasKey(k => new { k.Id });
        }

        public DbSet<ESportBet> ESportBet { get; set; }
        public DbSet<ESportBetAudit> ESportBetAudit { get; set; }
    }
}

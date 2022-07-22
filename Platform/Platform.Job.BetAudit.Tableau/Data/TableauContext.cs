using Microsoft.EntityFrameworkCore;

namespace Platform.Job.BetAudit.Tableau.Data
{
    public class TableauContext : DbContext
    {
        public TableauContext(DbContextOptions<TableauContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LastRunBetHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<LastRunBetSelectionHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetAuditHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAuditHK>().HasKey(k => new { k.Id });
        }

        public DbSet<LastRunBetHK> LastRunBetHK { get; set; }
        public DbSet<LastRunBetSelectionHK> LastRunBetSelectionHK { get; set; }
        public DbSet<BetAuditHK> BetAuditHK { get; set; }
        public DbSet<BetSelectionAuditHK> BetSelectionAuditHK { get; set; }
    }
}

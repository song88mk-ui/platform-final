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
            modelBuilder.Entity<LastRunBetHKFirst>().HasKey(k => new { k.Id });
            modelBuilder.Entity<LastRunBetSelectionHKFirst>().HasKey(k => new { k.Id });

            modelBuilder.Entity<LastRunBetHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<LastRunBetSelectionHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetAuditHK>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAuditHK>().HasKey(k => new { k.Id });

            modelBuilder.Entity<LastRunBetSA>().HasKey(k => new { k.Id });
            modelBuilder.Entity<LastRunBetSelectionSA>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetAuditSA>().HasKey(k => new { k.Id });
            modelBuilder.Entity<BetSelectionAuditSA>().HasKey(k => new { k.Id });
        }

        public DbSet<LastRunBetHKFirst> LastRunBetHKFirst { get; set; }
        public DbSet<LastRunBetSelectionHKFirst> LastRunBetSelectionHKFirst { get; set; }

        public DbSet<LastRunBetHK> LastRunBetHK { get; set; }
        public DbSet<LastRunBetSelectionHK> LastRunBetSelectionHK { get; set; }
        public DbSet<BetAuditHK> BetAuditHK { get; set; }
        public DbSet<BetSelectionAuditHK> BetSelectionAuditHK { get; set; }

        public DbSet<LastRunBetSA> LastRunBetSA { get; set; }
        public DbSet<LastRunBetSelectionSA> LastRunBetSelectionSA { get; set; }
        public DbSet<BetAuditSA> BetAuditSA { get; set; }
        public DbSet<BetSelectionAuditSA> BetSelectionAuditSA { get; set; }
    }
}

using LogData;
using Microsoft.EntityFrameworkCore;

namespace LogEntryDataAccess
{
	public partial class LoggingContext : DbContext
    {
        public LoggingContext()
        {
        }

        public LoggingContext(DbContextOptions<LoggingContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LogEntry> LogEntries { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=LoggingConnection");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.Property(expression => expression.TimeStamp).IsRequired(false);
                entity.Property(expression => expression.EventId).IsRequired(false);
                entity.Property(expression => expression.Category).IsRequired(true);
                entity.Property(expression => expression.LogLevel).IsRequired(true);
                entity.Property(expression => expression.Message).IsRequired(false);
                entity.Property(expression => expression.Exception).IsRequired(false);
                entity.Property(expression => expression.Payload).IsRequired(false);
                entity.Property(expression => expression.PayloadType).IsRequired(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

using LogData;
using Microsoft.EntityFrameworkCore;

namespace LogEntryDataAccess
{
	/// <summary>
	/// Database context, used by Entity Framework to access log data from the database. 
	/// </summary>
	/// <remarks>
	/// Execute the following command in the package manager console, to define the database and add mock data:
	/// <![CDATA[
	/// Update-Database -StartupProject LogEntryDataAccess -Project LogEntryDataAccess
	/// ]]>
	/// </remarks>
	public partial class LoggingContext : DbContext
    {
		/// <summary>
		/// Create a new database context, used by Entity Framework to access data from the database.
		/// </summary>
		public LoggingContext()
        {
        }

		/// <summary>
		/// Create a new database context, used by Entity Framework to access data from the database.
		/// </summary>
		/// <param name="options">Database connection options.</param>
        public LoggingContext(DbContextOptions<LoggingContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LogEntry> LogEntries { get; set; } = null!;

		/// <summary>
		/// Define how to connect to the database.
		/// </summary>
		/// <param name="optionsBuilder">Options used to configure the database connection.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=LoggingConnection");
            }
        }

		/// <summary>
		/// Define the database scheme used by the log database and add dummy data to it.
		/// </summary>
		/// <param name="modelBuilder">Builder used to alter the database schema.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.Property(expression => expression.Id).IsRequired(true).UseIdentityColumn();
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

			// Create initial mock data in the new database.
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 1, EventId = 1, TimeStamp = DateTime.Now.AddHours(-5), Category = "Disk space", Message = "Drive is low on disk space", LogLevel=LogLevel.Warning.ToString(), Payload = "17TB remaining", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 2, EventId = 1, TimeStamp = DateTime.Now.AddHours(-4), Category = "Disk space", Message = "Drive is still low on disk space", LogLevel=LogLevel.Warning.ToString(), Payload = "16TB remaining", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 3, EventId = 1, TimeStamp = DateTime.Now.AddHours(-3), Category = "Disk space", Message = "Drive is very low on disk space", LogLevel=LogLevel.Warning.ToString(), Payload = "6GB remaining", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 4, EventId = 1, TimeStamp = DateTime.Now.AddHours(-2), Category = "Disk space", Message = "Drive is out of disk space", LogLevel=LogLevel.Error.ToString(), Payload = "127B remaining", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 5, EventId = 2, TimeStamp = DateTime.Now.AddHours(-6), Category = "Memory", Message = "High memory usage", LogLevel=LogLevel.Information.ToString(), Payload = "180GB used", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 6, EventId = 2, TimeStamp = DateTime.Now.AddHours(-5), Category = "Memory", Message = "High memory usage", LogLevel=LogLevel.Information.ToString(), Payload = "189GB used", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 7, EventId = 2, TimeStamp = DateTime.Now.AddHours(-4), Category = "Memory", Message = "High memory usage", LogLevel=LogLevel.Information.ToString(), Payload = "204GB used", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 8, EventId = 2, TimeStamp = DateTime.Now.AddHours(-3), Category = "Memory", Message = "Very high memory usage", LogLevel=LogLevel.Warning.ToString(), Payload = "780GB used", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 9, EventId = 3, TimeStamp = DateTime.Now.AddHours(-6), Category = "Temperature", Message = "High CPU temperature", LogLevel=LogLevel.Information.ToString(), Payload = "50C", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 10, EventId = 3, TimeStamp = DateTime.Now.AddHours(-5), Category = "Temperature", Message = "High CPU temperature", LogLevel=LogLevel.Information.ToString(), Payload = "65C", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 11, EventId = 3, TimeStamp = DateTime.Now.AddHours(-4), Category = "Temperature", Message = "High CPU temperature", LogLevel=LogLevel.Information.ToString(), Payload = "69C (nice)", PayloadType="Text", Exception=null });
			modelBuilder.Entity<LogEntry>().HasData(new LogEntry() { Id = 12, EventId = 3, TimeStamp = DateTime.Now.AddHours(-3), Category = "Temperature", Message = "Very CPU temperature", LogLevel=LogLevel.Warning.ToString(), Payload = "99C", PayloadType="Text", Exception=null });
		}

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

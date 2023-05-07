using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace FilterTableExample.Data
{
    public partial class MyDataTypeContext : DbContext
    {
        public MyDataTypeContext()
        {
        }

        public MyDataTypeContext([NotNull] DbContextOptions<MyDataTypeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<MyDataType> MyDataTypes 
		{ 
			get; 
			set; 
		} = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=DatabaseConnection");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyDataType>(entity =>
            {
				entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn(); //ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired(true);
                entity.Property(e => e.Description).HasMaxLength(250).IsRequired(false);
                entity.Property(e => e.Price).HasPrecision(18, 2).IsRequired(true);
                entity.Property(e => e.ExpirationDate).IsRequired(false);
            });

            OnModelCreatingPartial(modelBuilder);

			// Create initial data in the database.
			modelBuilder.Entity<MyDataType>().HasData(new MyDataType() {Id = 1, Name = "Banana",	Description = "Yellow item used to measure for scale",		Price = 4.00M,	ExpirationDate = new DateTime(2023, 1, 1, 12, 0, 0)});
			modelBuilder.Entity<MyDataType>().HasData(new MyDataType() {Id = 2, Name = "Apple",		Description = "The forbidden fruit. Keep away from Eve",	Price = 2.00M,	ExpirationDate = new DateTime(0001, 1, 1, 12, 0, 0)});
			modelBuilder.Entity<MyDataType>().HasData(new MyDataType() {Id = 3, Name = "Wrench",	Description = "Adjust things that are out of alignment",	Price = 14.00M,	ExpirationDate = null});
			modelBuilder.Entity<MyDataType>().HasData(new MyDataType() {Id = 4, Name = "Paper",		Description = "More than one piece",						Price = 7.00M,	ExpirationDate = new DateTime(2100, 1, 1, 0, 0, 0)});
			modelBuilder.Entity<MyDataType>().HasData(new MyDataType() {Id = 5, Name = "Cheese",	Description = "PacMan likes this, a lot!",					Price = 19.25M,	ExpirationDate = new DateTime(2023, 02, 1, 12, 0, 0)});
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

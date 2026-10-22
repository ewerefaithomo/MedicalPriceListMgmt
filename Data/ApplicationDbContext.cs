using MedicalPriceListApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalPriceListApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PriceList> PriceLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PriceList>(entity =>
            {
                entity.ToTable("PriceList");

                // BANK_CODE is the primary key
                entity.HasKey(e => e.BANK_CODE);

                entity.Property(e => e.BANK_CODE)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.SERVICES)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.CURRENT_PRICE)
                    .HasPrecision(18, 2)
                    .IsRequired();

                entity.Property(e => e.REVENUE_GENERATING_FUND)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.DEPARTMENT)
                    .HasMaxLength(150)
                    .IsRequired();
            });
        }
    }
}
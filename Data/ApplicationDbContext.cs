using GaneshEnterprises.Models;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Retailer> Retailers { get; set; } = null!;
        public DbSet<OrderTracking> OrderTrackings { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(p => p.CustomerPrice)
                    .HasPrecision(18, 2);

                entity.Property(p => p.RetailerPrice)
                    .HasPrecision(18, 2);

                entity.Property(p => p.Price)
                    .HasPrecision(18, 2);

                entity.Property(p => p.BagSize)
                    .IsRequired();
            });

            // Configure Retailer entity
            modelBuilder.Entity<Retailer>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.BusinessName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(r => r.OwnerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(r => r.Email)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(r => r.Phone)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.CustomerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(o => o.ContactNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(o => o.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(o => o.TotalAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(o => o.Product)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(o => o.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Retailer)
                    .WithMany(r => r.Orders)
                    .HasForeignKey(o => o.RetailerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OrderTracking entity
            modelBuilder.Entity<OrderTracking>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.TrackingNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(t => t.CurrentStatus)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(t => t.CurrentLocation)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(t => t.CarrierName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(t => t.Order)
                    .WithMany(o => o.TrackingUpdates)
                    .HasForeignKey(t => t.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
                entity.Property(i => i.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(i => i.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

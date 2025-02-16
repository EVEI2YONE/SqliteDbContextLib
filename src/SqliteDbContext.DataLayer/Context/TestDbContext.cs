using Microsoft.EntityFrameworkCore;
using SqlDbContextLib.DataLayer.Domain;

namespace SqlDbContextLib.DataLayer.Context
{
    public class TestDbContext : DbContext
    {
        public TestDbContext() { }
        public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<Store> Stores { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Purchase> Purchases { get; set; }
        public virtual DbSet<Sale> Sales { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Region>(entity =>
            {
                entity.HasKey(e => e.RegionId);
                entity.HasMany(e => e.Stores)
                      .WithOne()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.StoreId);

                entity.HasOne(e => e.Region)
                      .WithMany(r => r.Stores)
                      .HasForeignKey(e => e.RegionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Sales)
                      .WithOne()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.SaleId);

                entity.HasOne(e => e.Store)
                      .WithMany(s => s.Sales)
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.DiscountBudgetUsed)
                      .HasConversion<double>()  // Fix for SQLite
                      .IsRequired();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Price)
                      .HasConversion<double>()  // Fix for SQLite
                      .IsRequired();
            });

            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.HasKey(e => e.PurchaseId);

                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Purchases)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Store)
                      .WithOne()
                      .HasForeignKey<Purchase>(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithOne()
                      .HasForeignKey<Purchase>(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);

                entity.HasMany(e => e.Purchases)
                      .WithOne()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Name).IsRequired();
            });

            

            
        }
    }
}

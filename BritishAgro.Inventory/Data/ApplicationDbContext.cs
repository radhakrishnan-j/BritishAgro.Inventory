using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BritishAgro.Inventory.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<StoreProductLot> StoreProductLots => Set<StoreProductLot>();
        public DbSet<ProductUsage> ProductUsages => Set<ProductUsage>();
        public DbSet<ProductReturn> ProductReturns => Set<ProductReturn>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Category>()
                .HasMany(category => category.Products)
                .WithOne(product => product.Category)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Product>()
                .HasMany(product => product.StoreStocks)
                .WithOne(lot => lot.Product)
                .HasForeignKey(lot => lot.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasMany(product => product.ProductUsages)
                .WithOne(usage => usage.Product)
                .HasForeignKey(usage => usage.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasMany(product => product.ProductReturns)
                .WithOne(productReturn => productReturn.Product)
                .HasForeignKey(productReturn => productReturn.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductUsage>()
                .HasMany(usage => usage.Returns)
                .WithOne(productReturn => productReturn.ProductUsage)
                .HasForeignKey(productReturn => productReturn.UsageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProductUsage>()
                .HasMany(usage => usage.StoreProductLots)
                .WithOne(lot => lot.ProductUsage)
                .HasForeignKey(lot => lot.UsageId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

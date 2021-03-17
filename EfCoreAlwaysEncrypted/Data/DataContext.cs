using EfCoreAlwaysEncrypted.Model;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAlwaysEncrypted.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Property(product => product.Name).HasMaxLength(50).IsRequired(false);
            modelBuilder.Entity<Product>().Property(product => product.Description).HasMaxLength(500).IsRequired(false);
            modelBuilder.Entity<Product>().Property(product => product.Price).HasPrecision(10, 2);
            modelBuilder.Entity<Product>().Property(product => product.DiscountPercentage).HasPrecision(5, 2).IsRequired(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}

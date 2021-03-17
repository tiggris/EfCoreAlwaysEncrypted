using EfCoreAlwaysEncrypted.Data;
using EfCoreAlwaysEncrypted.Model;
using EfCoreAlwaysEncrypted.Tests.Utils;
using Xunit;

namespace EfCoreAlwaysEncrypted.Tests
{
    [Collection("Always encrypted collection")]
    public class AlwaysEncryptedTests
    {
        [Fact]
        public void InsertProduct_WhenDiscountIsNull_ShouldNotThrowException()
        {
            // Arrange
            var product = new Product
            {
                Name = "Some product",
                Description = "Some description",
                Price = 123.00m
            };
            
            // Act
            using (var dataContext = new DataContext(DatabaseFixture.DbContextOptions))
            {
                dataContext.Products.Add(product);
                dataContext.SaveChanges();
            }

            // Assert
            using (var dataContext = new DataContext(DatabaseFixture.DbContextOptions))
            {
                var savedProduct = dataContext.Products.Find(product.Id);
                Assert.NotNull(savedProduct);
            }
        }

        [Fact]
        public void InsertProduct_WhenDescriptionIsNull_ShouldNotThrowException()
        {
            // Arrange
            var product = new Product
            {
                Name = "Some product",
                Price = 123.00m,
                DiscountPercentage = 15.50m
            };

            // Act
            using (var dataContext = new DataContext(DatabaseFixture.DbContextOptions))
            {
                dataContext.Products.Add(product);
                dataContext.SaveChanges();
            }

            // Assert
            using (var dataContext = new DataContext(DatabaseFixture.DbContextOptions))
            {
                var savedProduct = dataContext.Products.Find(product.Id);
                Assert.NotNull(savedProduct);
            }
        }
    }
}

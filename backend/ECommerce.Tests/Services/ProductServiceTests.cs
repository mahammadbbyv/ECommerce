using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductService _productService;
    private readonly Category _testCategory;

    public ProductServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _productService = new ProductService(_context, MockLogger.Create<ProductService>());

        // Setup test category
        _testCategory = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(_testCategory);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Arrange
        var products = new[]
        {
            new Product { Name = "Laptop", Price = 999.99m, StockQuantity = 10, CategoryId = _testCategory.Id },
            new Product { Name = "Mouse", Price = 29.99m, StockQuantity = 50, CategoryId = _testCategory.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetAllProductsAsync(null, null);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithCategoryFilter_ShouldReturnFilteredProducts()
    {
        // Arrange
        var category2 = new Category { Name = "Books", Description = "Books and magazines" };
        _context.Categories.Add(category2);
        await _context.SaveChangesAsync();

        var products = new[]
        {
            new Product { Name = "Laptop", Price = 999.99m, StockQuantity = 10, CategoryId = _testCategory.Id },
            new Product { Name = "Book", Price = 19.99m, StockQuantity = 100, CategoryId = category2.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetAllProductsAsync(_testCategory.Id, null);
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.Equal("Laptop", resultList[0].Name);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithSearchTerm_ShouldReturnMatchingProducts()
    {
        // Arrange
        var products = new[]
        {
            new Product { Name = "Gaming Laptop", Description = "High-end gaming", Price = 1499.99m, StockQuantity = 5, CategoryId = _testCategory.Id },
            new Product { Name = "Office Laptop", Description = "Business laptop", Price = 799.99m, StockQuantity = 15, CategoryId = _testCategory.Id },
            new Product { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, StockQuantity = 50, CategoryId = _testCategory.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act - Search is case-sensitive, so use "Laptop" with capital L
        var result = await _productService.GetAllProductsAsync(null, "Laptop");
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, p => Assert.Contains("Laptop", p.Name));
    }

    [Fact]
    public async Task GetProductByIdAsync_ExistingProduct_ShouldReturnProduct()
    {
        // Arrange
        var product = new Product
        {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 999.99m,
            StockQuantity = 10,
            CategoryId = _testCategory.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Name, result.Name);
        Assert.Equal(product.Price, result.Price);
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistentProduct_ShouldReturnNull()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProductAsync_ValidProduct_ShouldCreateAndReturnProduct()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Laptop",
            Description = "Latest model",
            Price = 1299.99m,
            StockQuantity = 20,
            CategoryId = _testCategory.Id,
            ImageUrl = "https://example.com/laptop.jpg"
        };

        // Act
        var result = await _productService.CreateProductAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Price, result.Price);

        var productInDb = await _context.Products.FirstOrDefaultAsync(p => p.Name == createDto.Name);
        Assert.NotNull(productInDb);
    }

    [Fact]
    public async Task CreateProductAsync_InvalidCategory_ShouldThrowException()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Laptop",
            Description = "Latest model",
            Price = 1299.99m,
            StockQuantity = 20,
            CategoryId = 999,
            ImageUrl = "https://example.com/laptop.jpg"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _productService.CreateProductAsync(createDto));
        Assert.Equal("Category not found", exception.Message);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidProduct_ShouldUpdateAndReturn()
    {
        // Arrange
        var product = new Product
        {
            Name = "Old Laptop",
            Description = "Old model",
            Price = 799.99m,
            StockQuantity = 10,
            CategoryId = _testCategory.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProductDto
        {
            Name = "Updated Laptop",
            Description = "Updated description",
            Price = 899.99m,
            StockQuantity = 15,
            CategoryId = _testCategory.Id,
            ImageUrl = "https://example.com/updated.jpg"
        };

        // Act
        var result = await _productService.UpdateProductAsync(product.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Price, result.Price);
    }

    [Fact]
    public async Task UpdateProductAsync_NonExistentProduct_ShouldThrowException()
    {
        // Arrange
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Laptop",
            Description = "Updated description",
            Price = 899.99m,
            StockQuantity = 15,
            CategoryId = _testCategory.Id
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _productService.UpdateProductAsync(999, updateDto));
        Assert.Equal("Product not found", exception.Message);
    }

    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_ShouldReturnTrue()
    {
        // Arrange
        var product = new Product
        {
            Name = "Laptop to Delete",
            Price = 999.99m,
            StockQuantity = 10,
            CategoryId = _testCategory.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.DeleteProductAsync(product.Id);

        // Assert
        Assert.True(result);
        var productInDb = await _context.Products.FindAsync(product.Id);
        Assert.Null(productInDb);
    }

    [Fact]
    public async Task DeleteProductAsync_NonExistentProduct_ShouldReturnFalse()
    {
        // Act
        var result = await _productService.DeleteProductAsync(999);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

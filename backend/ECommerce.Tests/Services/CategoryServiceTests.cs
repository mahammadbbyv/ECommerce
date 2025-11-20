using ECommerce.API.Data;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Services.Classes;
using ECommerce.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _context = TestDbContext.CreateInMemoryContext();
        _categoryService = new CategoryService(_context, MockLogger.Create<CategoryService>());
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new[]
        {
            new Category { Name = "Electronics", Description = "Electronic devices" },
            new Category { Name = "Books", Description = "Books and magazines" },
            new Category { Name = "Clothing", Description = "Apparel and accessories" }
        };
        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();
        var resultList = result.ToList();

        // Assert
        Assert.Equal(3, resultList.Count);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_ShouldIncludeProductCount()
    {
        // Arrange
        var category = new Category { Name = "Electronics", Description = "Electronic devices" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var products = new[]
        {
            new Product { Name = "Laptop", Price = 999.99m, StockQuantity = 10, CategoryId = category.Id },
            new Product { Name = "Mouse", Price = 29.99m, StockQuantity = 50, CategoryId = category.Id }
        };
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetAllCategoriesAsync();
        var resultList = result.ToList();

        // Assert
        Assert.Single(resultList);
        Assert.Equal(2, resultList[0].ProductCount);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ExistingCategory_ShouldReturnCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetCategoryByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Name, result.Name);
        Assert.Equal(category.Description, result.Description);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_NonExistentCategory_ShouldReturnNull()
    {
        // Act
        var result = await _categoryService.GetCategoryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCategoryAsync_ValidCategory_ShouldCreateAndReturn()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "New Category",
            Description = "New category description"
        };

        // Act
        var result = await _categoryService.CreateCategoryAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Description, result.Description);

        var categoryInDb = await _context.Categories.FirstOrDefaultAsync(c => c.Name == createDto.Name);
        Assert.NotNull(categoryInDb);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateName_ShouldThrowException()
    {
        // Arrange
        var existingCategory = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var createDto = new CreateCategoryDto
        {
            Name = "Electronics",
            Description = "Different description"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.CreateCategoryAsync(createDto));
        Assert.Equal("A category with this name already exists", exception.Message);
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateNameCaseInsensitive_ShouldThrowException()
    {
        // Arrange
        var existingCategory = new Category
        {
            Name = "Electronics",
            Description = "Electronic devices"
        };
        _context.Categories.Add(existingCategory);
        await _context.SaveChangesAsync();

        var createDto = new CreateCategoryDto
        {
            Name = "ELECTRONICS",
            Description = "Different description"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _categoryService.CreateCategoryAsync(createDto));
        Assert.Equal("A category with this name already exists", exception.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
